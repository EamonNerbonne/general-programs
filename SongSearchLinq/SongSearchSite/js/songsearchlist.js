
$(document).ready(function ($) {
    function StripUriQueryHash(url) {
        var qIdx = url.indexOf("?");
        if (qIdx != -1) url = url.substring(0, qIdx);
        var hIdx = url.indexOf("#");
        if (hIdx != -1) url = url.substring(0, hIdx);
        return url;
    }
    function GetExtension(url) {
        url = decodeURIComponent(StripUriQueryHash(url));
        dotIdx = url.lastIndexOf(".");
        if (dotIdx == -1) return null;
        else return url.substring(dotIdx);
    }
    function GetFileName(url) {
        url = decodeURIComponent(StripUriQueryHash(url));
        var dotIdx = url.lastIndexOf(".");
        if (dotIdx != -1) url = url.substring(0, dotIdx);
        var slashIdx = url.lastIndexOf("/");
        if (slashIdx != -1) url = url.substring(slashIdx);
        return url;
    }

    function GuessMimeFromExtension(extension) {
        if (extension)
            switch (extension.toLowerCase()) {
            case ".mp3": return "audio/mpeg";
            case ".wma": return "audio/x-ms-wma";
            case ".wav": return "audio/wav";
            case ".ogg": return "audio/ogg";
            case ".mpc":
            case ".mpp":
            case ".mp+": return "audio/x-musepack";
        }
        return null;
    }

    function UriToMime(uri) { return GuessMimeFromExtension(GetExtension(uri)); }

    var playlistItem = null;

    var useNotifications = window.webkitNotifications && window.webkitNotifications.checkPermission() == 0;
    var hasBeenNotified = false;
    var userOptions = {
        serializedList: {
            label: "Serialized Playlist",
            type: "textbox",
            initialValue: "",
            onchange: function (newval, codeTriggered, e) {
                if (codeTriggered) return;
                var val = false;
                try { val = JSON.parse(newval) } catch (e) { }
                if (val) loadPlaylist(val);
            }
        }
        /*  , lfmUser: {
        label: "Last.FM username",
        type: "textbox",
        initialValue: ""
        }
        , lfmPass: {
        label: "Last.FM password",
        type: "textbox",
        initialValue: ""
        }
        , scrobble: {
        label: "Enable Scrobbling",
        type: "checkbox",
        initialValue: false,
        onchange: function (newval, codeTriggered, e) {
        if (newval && playlistItem) {
        var songdata = $(playlistItem).data("songdata");
        alert(JSON.stringify(songdata));
        $.ajax({
        type: "POST",
        url: "scrobble",
        data:
        {
        scrobbler: JSON.stringify(
        {
        user: userOptions.lfmUser.getValue()
        , pass: userOptions.lfmPass.getValue()
        , href: songdata.href
        , label: songdata.label
        })
        },
        timeout: 10000,
        success: function (data) { alert(JSON.stringify(data)); },
        error: function (xhr, status, errorThrown) { alert(JSON.stringify([xhr, status, errorThrown])); }
        });

        alert(songdata.href + ":" + songdata.length + ":" + songdata.label);
        }
        }
        }
        */

        //        , playlistName: {
        //            label: "Playlist Name",
        //            type: "textbox",
        //            initialValue: "",
        //            onchange: function (newval, codeTriggered, e) {
        //                //                if (codeTriggered) return;
        //                //                var val = false;
        //                //                try { val = JSON.parse(newval) } catch (e) { }
        //                //                if (val) loadPlaylist(val);
        //            }
        //}
    };

    if (window.webkitNotifications) {
        userOptions.notifications = {
            label: "Desktop Notification",
            type: "checkbox",
            initialValue: useNotifications,
            onchange: function (newval, codeTriggered, e) {
                if (newval && window.webkitNotifications.checkPermission() != 0) {
                    var opt = this;
                    window.webkitNotifications.requestPermission(function () { useNotifications = window.webkitNotifications.checkPermission() == 0; opt.setValue(useNotifications); });
                    opt.setValue(window.webkitNotifications.checkPermission() == 0);
                } else
                    useNotifications = newval;
            }
        }
    }
    if (!$.isEmptyObject(userOptions)) {
        $("#optionsBox").OptionsBuilder(userOptions);
        userOptions.serializedList.element.click(function (e) {
            userOptions.serializedList.element.select();
            return false;
        });
    }



    // Local copy of jQuery selectors, for performance.
    var jpPlayTime = $("#jplayer_play_time");
    var jpTotalTime = $("#jplayer_total_time");
    function playlistClick(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedListItem = $(target).parents().andSelf().filter("li").first()[0];
        var clickedDelete = $(target).parents().andSelf().filter(".deleteButton").length > 0;
        var clickedRatingEl = $(target).parents().andSelf().filter("div[data-rating]");


        if (clickedDelete)
            playlistDelete(clickedListItem);
        else if (clickedRatingEl.length > 0) {
            var clickedA = $(target).parents().andSelf().filter("a").first();
            var newRating = clickedA.parents().filter("div[data-rating]").length == 0 ? 0
                : clickedA.prevAll().length;
            updateRating(clickedListItem, newRating);
        } else if (e.type == "dblclick") {
            if (document.selection && document.selection.empty)
                document.selection.empty();
            else if (window.getSelection) {
                var selection = window.getSelection();
                if (selection && selection.removeAllRanges)
                    selection.removeAllRanges();
            }
            playlistChange(clickedListItem);
        }
    }

    function updateRating(clickedListItem, newRating) {
        var jqItem = $(clickedListItem);
        var songdata = jqItem.data("songdata");
        var ratingDiv = jqItem.children().filter("div[data-rating]");
        var oldRating = songdata.rating;
        songdata.rating = newRating;
        ratingDiv.attr("data-rating", newRating || 0);
        playlistToLocalStorage();

        function oops(ignore, errorstatus, errormessage) {
            songdata.rating = oldRating;
            ratingDiv.attr("data-rating", oldRating || 0);
            alert(errorstatus + " while setting rating=" + newRating + ": " + songdata.href + "\n" + (errormessage || ""));
        };

        $.ajax({
            type: "POST",
            url: "update-rating",
            data: { songuri: songdata.href, rating: newRating },
            timeout: 62 * 1000,
            success: function (data) { if (data && data.error) oops(null, data.error, data.message); },
            error: oops
        });
    }


    var playlistContainer = $("#jplayer_playlist");
    var playlistElem = null;

    $("#jquery_jplayer").jPlayer({
        ready: function () {
            playlistElem = $(document.createElement("ul"))
                    .appendTo(playlistContainer.empty())
                    .click(playlistClick).dblclick(playlistClick); //TODO: enable drag/drop.
            playlistDragnDropInit();

            $("#similar .known").click(knownClick);
            loadPlaylist(JSON.parse(localStorage.playlist));
        },
        oggSupport: true,
        swfPath: ""
    })
	.jPlayer("onProgressChange", function (loadPercent, playedPercentRelative, playedPercentAbsolute, playedTime, totalTime) {
	    jpPlayTime.text($.jPlayer.convertTime(playedTime));
	    jpTotalTime.text($.jPlayer.convertTime(totalTime));
	    if (useNotifications && !hasBeenNotified) {
	        hasBeenNotified = true;
	        if (window.webkitNotifications.checkPermission() != 0)
	            userOptions.notifications.setValue(useNotifications = false);
	        else
	            ShowPopup($(playlistItem).contents(":empty").text());
	    }
	})
	.jPlayer("onSoundComplete", function () {
	    playlistNext();
	});

    var popupCount = 0;
    var outstandingNotifications = {};
    function CancelPopup(index) {
        var oldNotification = outstandingNotifications[index];
        if (!oldNotification) return;
        delete outstandingNotifications[index];
        oldNotification.cancel();
    }
    function ShowPopup(songTitle) {
        var popup = window.webkitNotifications.createNotification("img/emnicon.png", songTitle, songTitle);
        var popupI = popupCount++;
        outstandingNotifications[popupI] = popup;
        popup.ondisplay = function () { setTimeout(function () { CancelPopup(popupI); }, 5000); }
        popup.show();
    }

    window.addEventListener("unload", function () {
        for (var pI in outstandingNotifications)
            CancelPopup(pI);
    });

    $("#jplayer_previous").click(function () {
        playlistPrev();
        return false;
    });

    $("#jplayer_next").click(function () {
        playlistNext();
        return false;
    });

    function emptyPlaylist() {
        playlistChange(null);
        playlistElem.empty();
        playlistRefreshUi();
    }

    function loadPlaylistSync(list) {
        playlistElem.empty();
        for (i = 0; i < list.length; i++)
            addToPlaylistRaw(list[i]);
        if (list.length > 0) playlistChange($("#jplayer_playlist ul li")[0]);
        else $("#jquery_jplayer").jPlayer("stop");
        playlistRefreshUi();
    }

    function loadPlaylist(list) {
        function oops(ignore, errorstatus, errormessage) {
            alert(errorstatus + " while bouncing playlist off server\n" + (errormessage || ""));
        };

        $.ajax({
            type: "POST",
            url: "bounce-playlist",
            data: { playlist: JSON.stringify(list), format: "json" },
            timeout: 62 * 1000,
            success: function (data) { if (data && data.error) oops(null, data.error, data.message); else loadPlaylistSync(data); },
            error: oops
        });
    }


    function savePlaylist() {
        return $("#jplayer_playlist ul li").map(function (i, e) { return $(e).data("songdata"); }).get();
    }

    function repeatStr(str, times) { return Array(times + 1).join(str); }

    function toNestedAnchors(str, idx) {
        return idx >= str.length ? null : $(document.createElement("a")).append(toNestedAnchors(str, idx + 1)).append($(document.createTextNode(str[str.length - idx - 1])));
    }

    function appendPerCharAnchor(el, str) {
        for (var i = 0; i < str.length; i++)
            el.append($(document.createElement("a")).text(str[i]));
        return el;
    }
    var sixAnchors = $("<a><a><a><a><a><a></a></a></a></a></a></a>");

    function getLabel(song) {
        return song.artist && song.title ? song.artist + " - " + song.title : song.label;
    }

    function makeListItem(song) {
        var rating = Math.max(0, Math.min(song.rating || 0, 5));
        return $(document.createElement("li")).text(getLabel(song)).attr("draggable", "true").data("songdata", song)
            .append($(document.createElement("div")).attr("data-rating", rating).append(sixAnchors.clone()))
            .append($(document.createElement("div")).addClass("deleteButton"));
    }

    function addToPlaylistRaw(song) {
        var listItem = makeListItem(song);
        listItem.appendTo(playlistElem);
        return listItem[0];
    }

    function addToPlaylist(song) {
        var shouldStart = playlistElem.children().length == 0;
        var newItem = addToPlaylistRaw(song);
        playlistRefreshUi();
        if (shouldStart) playlistChange(newItem);
        else scrollIntoMiddleView(newItem);
    }

    function playlistToLocalStorage() {
        var serializedList = JSON.stringify(savePlaylist());
        userOptions.serializedList.setValue(serializedList);
        localStorage.setItem("playlist", serializedList);               // defining the localStorage variable 
    }

    function playlistRefreshUi() {
        playlistToLocalStorage();
        simStateSet.getting();
    }

    var isGetQueued = false;
    var leftColSel = $(".similarItemsDisplay"), knownSel = $("#similar .known"), unknownSel = $("#similar .unknown");

    function updateSimilarDisplay(data) {
        var known = data.known, unknown = data.unknown;
        unknownSel.empty();
        for (var i = 0; i < unknown.length; i++)
            unknownSel.append($(document.createElement("li")).text(unknown[i]));
        knownSel.empty();
        leftColSel.removeClass("waiting");
        for (var i = 0; i < known.length; i++) {
            knownSel.append(
                $(document.createElement("li"))
                    .text(getLabel(known[i]))
                    .attr("data-staticrating", known[i].rating || "")
                    .data("songdata", known[i])
                    .append(
                        $(document.createElement("div"))
                            .append(
                                $(document.createElement("div"))
                                    .attr("class", "popAbar")
                                    .attr("style", "width: " + (known[i].popA * 5) + "em")
                            )
                            .append(
                                $(document.createElement("div"))
                                    .attr("class", "popTbar")
                                    .attr("style", "width: " + (known[i].popT * 5) + "em")
                            )
                    )
            );
        }
    }


    var simStateSet = {
        getting: function () {
            if (simStateSet.current == "getting")
                isGetQueued = true;
            else {
                isGetQueued = false;
                simStateSet.current = "getting";
                leftColSel.removeClass("proc-error");
                leftColSel.removeAttr("data-errname");
                leftColSel.removeAttr("data-errtrace");
                leftColSel.removeAttr("data-errmsg");
                leftColSel.addClass("processing");
                getSimilarImpl();
            }
        },
        done: function (data) {
            if (!data || data.error || !data.known || !data.unknown) { this.error("data-error", false, data); }
            else {
                if (mouseInSimilar) { simDispDataWait = data; leftColSel.addClass("waiting"); }
                else updateSimilarDisplay(data);
                leftColSel.attr("data-lookups", data.lookups);
                leftColSel.attr("data-weblookups", data.weblookups);
                leftColSel.attr("data-milliseconds", data.milliseconds);

                if (simStateSet.current == "done") return;
                simStateSet.current = "done";
                leftColSel.removeClass("proc-error");
                leftColSel.removeAttr("data-errname");
                leftColSel.removeAttr("data-errtrace");
                leftColSel.removeAttr("data-errmsg");
                leftColSel.removeClass("processing");
                if (isGetQueued)
                    simStateSet.getting();
            }
        },
        error: function (status, retry, errordetail) {
            dataStatus = status || dataStatus;
            isGetQueued = isGetQueued || retry;
            if (errordetail) {
                leftColSel.attr("data-errmsg", errordetail.message);
                leftColSel.attr("data-errtrace", errordetail.fulltrace);
                leftColSel.attr("data-errname", errordetail.error);
            } else leftColSel.attr("data-errname", dataStatus);

            if (simStateSet.current == "error") return;
            simStateSet.current = "error";
            leftColSel.addClass("proc-error");
            leftColSel.removeClass("processing");
            leftColSel.removeAttr("data-lookups");
            if (isGetQueued)
                simStateSet.getting();
        },
        current: "done"
    }

    var simDispDataWait = null, dataStatus = null;

    var mouseInSimilar = false;
    knownSel.hover(function (e) { mouseInSimilar = true; }, function (e) {
        mouseInSimilar = false;
        if (simDispDataWait) {
            updateSimilarDisplay(simDispDataWait);
            simDispDataWait = null;
        }
    });


    function getSimilarImpl() {
        $.ajax({
            type: "POST",
            url: "similar-to",
            data: { playlist: JSON.stringify(savePlaylist()) },
            timeout: 10000,
            success: function (data) { simStateSet.done(data); },
            error: function (xhr, status, errorThrown) { simStateSet.error(status, status == "timeout"); }
        });
    }

    function scrollIntoMiddleView(listItemElem) {
        var s0 = playlistContainer[0].scrollTop;
        var sD = playlistContainer[0].clientHeight;
        var i0 = listItemElem.offsetTop;
        var iD = listItemElem.clientHeight;
        if (i0 < s0 || i0 + iD > s0 + sD) {//not entirely in view
            //middle: s0 + sD/2 == i0+iD/2  ==> s0 = i0+iD/2-sD/2
            playlistContainer[0].scrollTop = i0 + (iD - sD) / 2;
        }
    }

    function playlistConfig(listItem) {
        if (playlistItem)
            $(playlistItem).removeClass("jplayer_playlist_current");
        playlistItem = listItem;
        if (playlistItem) {
            $(playlistItem).addClass("jplayer_playlist_current");
            hasBeenNotified = false;
            var songdata = $(playlistItem).data("songdata");
            $("#jquery_jplayer").jPlayer("loadSong", [{ type: UriToMime(songdata.href), src: songdata.href, replaygain: songdata.replaygain}]);
            scrollIntoMiddleView(playlistItem);
        }
    }

    function playlistChange(newCurrentItem) {
        if (newCurrentItem != playlistItem)
            playlistConfig(newCurrentItem);
        if (playlistItem)
            $("#jquery_jplayer").jPlayer("play");
        else
            $("#jquery_jplayer").jPlayer("stop");
    }

    function playlistDelete(listItem) {
        if (listItem == playlistItem)
            playlistChange(null);
        $(listItem).remove();
        playlistRefreshUi();
    }

    $("#do_shuffle").click(function do_shuffle_click() {
        var kids = $.makeArray(playlistElem.children());
        var kidCount = kids.length;
        for (var i = kidCount - 1; i >= 0; i--) {
            $(kids[i]).detach();
        }
        for (var i = kidCount - 1; i >= 0; i--) {
            var newpos = Math.floor(Math.random() * (i + 1) % (i + 1));
            var oldItem = kids[newpos];
            kids[newpos] = kids[i];
            kids[i] = oldItem;
        }
        for (var i = 0; i < kidCount; i++) {
            $(kids[i]).appendTo(playlistElem);
        }
    });

    $("#do_addAll").click(function do_add_all() {
        var allRows = $("#resultsview").contents().find("tr[data-href]");
        allRows.each(function (index, row) {
            addRowToPlaylist($(row));
        });
    });


    $("#do_removeAll").click(emptyPlaylist);

    function playlistNext() { playlistChange($(playlistItem).next()[0] || $("#repeat_playlist_box:checked")[0] && $("#jplayer_playlist ul li")[0]); }

    function playlistPrev() { playlistChange($(playlistItem).prev()[0]); }

    function knownClick(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedListItem = $(target).parents().andSelf().filter("li").first();
        addToPlaylist(clickedListItem.data("songdata"));
    }

    function addRowToPlaylist(clickedRowJQ) {
        addToPlaylist(
            { label: clickedRowJQ.attr("data-label") || GetFileName(clickedRowJQ.attr("data-href")),
                artist: clickedRowJQ.attr("data-artist"),
                title: clickedRowJQ.attr("data-title"),
                href: clickedRowJQ.attr("data-href"),
                length: clickedRowJQ.attr("data-length"),
                replaygain: Number(clickedRowJQ.attr("data-replaygain")) || 0,
                rating: Number(clickedRowJQ.attr("data-rating")),
                popA: Number(clickedRowJQ.attr("data-popA")),
                popT: Number(clickedRowJQ.attr("data-popT"))
            });
    }

    window.SearchListClicked = function SearchListClicked_impl(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedRow = $(target).parents("tr");
        if (clickedRow.length != 1)
            return;
        addRowToPlaylist(clickedRow);
    };

    window.SetOrdering = function SetOrdering_impl(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedHead = $(target).parents().andSelf().filter("th");

        if (clickedHead.length != 1)
            return;
        var clickedCol = clickedHead.attr("data-colname");
        var currOrder = $(target).parents("body").attr("data-ordering");
        $("#orderingEl").attr("value", clickedCol + ":" + currOrder);
        UpdateSongSearchResults();
    };

    $("#savePlaylistForm").submit(function savePlaylistAsM3u() {
        var serializedList = JSON.stringify(savePlaylist());
        $("#savePlaylistHiddenJson").val(JSON.stringify(savePlaylist()));
    });

    //////////////////////////////////////////////////////////////////////////////////////////////////DROP PLAYLIST


    window.globalDropHandler = function (e) {
        e.stopPropagation();
        e.preventDefault();
        var files = e.dataTransfer.files;
        //for (var i = 0, f; f = files[i]; i++) alert(f.name);
        if (!files.length) return true;

        function oops(ignore, errorstatus, errormessage) {
            alert(errorstatus + " while bouncing playlist off server\n" + (errormessage || ""));
        };
        function uploadDone(e) {
            if (e.target.status < 200 || e.target.status >= 300)
                oops(null, e.target.status, e.target.responseText);
            else {
                var jsResponse;
                try {
                    jsResponse = JSON.parse(e.target.responseText);
                } catch (ex) {
                    oops(null, "JSON.parse", ex.Description);
                }
                if (jsResponse.error)
                    oops(null, jsResponse.error, jsResponse.message);
                else if (jsResponse)
                    loadPlaylistSync(jsResponse);
            }
        }

        var xhr = new XMLHttpRequest();

        var fd = new FormData();
        fd.append("format", "json");
        fd.append("playlist", files[0]);
        xhr.addEventListener("load", uploadDone, false);
        xhr.addEventListener("error", oops, false);
        xhr.addEventListener("abort", oops, false);
        xhr.open("POST", "bounce-playlist");
        xhr.send(fd);
    };

    $("body").bind("dragover", function (e) {
        var dt = e.originalEvent.dataTransfer;
        e.preventDefault();
        e.originalEvent.dataTransfer.dropEffect = "copy";
    }).bind("drop", function (e) {
        globalDropHandler(e.originalEvent);
    });

    function blalog(text) {
        $("#similar").append($(document.createElement("div")).text(text));
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////DRAGNDROP PLAYLIST ITEMS
    function playlistDragnDropInit() { //playlist is set up!
        playlistElem.bind('dragstart', function (e) {
            var draggedItem = $(e.originalEvent.target).parents().andSelf().filter("li").first();
            var songdata = draggedItem.data("songdata");
            if (!songdata) return true;
            var dt = e.originalEvent.dataTransfer;
            dt.setData("text/plain", getLabel(songdata));
            dt.setData("Text", JSON.stringify({ song: songdata, position: draggedItem.prevAll().length }));
            dt.setData("application/x-song", JSON.stringify({ song: songdata, position: draggedItem.prevAll().length }));
            dt.setData("text/uri-list", songdata.href);
            return true;
        });
        playlistElem.parent().bind("dragover", function (e) {
            var dt = e.originalEvent.dataTransfer;
            var draggedData = dt.getData("application/x-song");
            if (!draggedData) return true;
            dt.dropEffect = "move";
            e.preventDefault();
            e.stopPropagation();
        });
        playlistElem.parent().bind('drop', function (e) {
            var dt = e.originalEvent.dataTransfer;

            var draggedRawData = dt.getData("application/x-song") || dt.getData("Text");
            if (!draggedRawData) return true;
            try {
                draggedRawData = JSON.parse(draggedRawData);
            } catch (ex) {
                return true;
            }
            var draggedData = draggedRawData.song;
            if (!draggedData)
                return true;
            e.stopPropagation();
            e.preventDefault();
            var target = e.originalEvent.target;
            var droppedOnListItem = $(target).parents().andSelf().filter("li").first();


            var fromListItem = playlistElem.children().eq(draggedRawData.position);
            var draggedFromThisList = fromListItem.length && fromListItem.data("songdata").href == draggedData.href;
            var droppedOnLi = droppedOnListItem.length && droppedOnListItem.data("songdata");

            if (draggedFromThisList) {
                fromListItem.detach();
                if (droppedOnLi)
                    fromListItem.insertAfter(droppedOnListItem); //dropped on LI
                else
                    playlistElem.append(fromListItem); //dropped on UL.
            } else {
                if (droppedOnLi) //dropped on LI from other browser
                    droppedOnListItem.after(makeListItem(draggedData));
                else
                    playlistElem.append(makeListItem(draggedData));
            }

            playlistRefreshUi();
        });


    }
});

