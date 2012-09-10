$(document).ready(function ($) {
    ///////////////////////////////////////////////////////////////////////////////////////////general javascript comparison function

    function unwrapStringOrNumber(obj) {
        return obj instanceof Number || obj instanceof String ? obj.valueOf() : obj;
    }
    function areEquivalent(a, b) { //does not support recursive structures.
        a = unwrapStringOrNumber(a);
        b = unwrapStringOrNumber(b);
        if (a === b) return true; //e.g. a and b both null
        if (a === null || b === null || typeof (a) !== typeof (b)) return false;
        if (a instanceof Date) return b instanceof Date && a.valueOf() === b.valueOf();
        if (typeof (a) !== "object") return a == b; //for boolean, number, string, xml

        var newA = (a.areEquivalent_Eq_91_2_34 === undefined),
            newB = (b.areEquivalent_Eq_91_2_34 === undefined);
        try {
            if (newA) a.areEquivalent_Eq_91_2_34 = [];
            else if (a.areEquivalent_Eq_91_2_34.some(function (other) { return other === b; })) return true;
            if (newB) b.areEquivalent_Eq_91_2_34 = [];
            else if (b.areEquivalent_Eq_91_2_34.some(function (other) { return other === a; })) return true;
            a.areEquivalent_Eq_91_2_34.push(b);
            b.areEquivalent_Eq_91_2_34.push(a);

            var tmp = {};
            for (var prop in a) if (prop != "areEquivalent_Eq_91_2_34") tmp[prop] = null;
            for (var prop in b) if (prop != "areEquivalent_Eq_91_2_34") tmp[prop] = null;

            for (var prop in tmp) if (!areEquivalent(a[prop], b[prop])) {
                //console.log(prop + ":" + JSON.stringify(a[prop]) + "!=" + JSON.stringify(b[prop])); 
                return false;
            }
            return true;
        } finally {
            if (newA) delete a.areEquivalent_Eq_91_2_34;
            if (newB) delete b.areEquivalent_Eq_91_2_34;
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////URL helpers

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

    ////////////////////////////////////////////////////////////////////////////////playlist+jplayer hookup

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
                if (val && val.list) loadPlaylistAsync(val);
                else if (val) loadPlaylistAsync({ name: null, list: val });
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
    var jpPlayTime_LastText;
    var jpTotalTime = $("#jplayer_total_time");
    var jpTotalTime_LastText;
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
        playlistStorage.contentsChanged(savePlaylist_ExtractFromHtml());

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

            $("#similar #similarKnown").click(knownClick);

            playlistStorage.loadLastList();
        },
        oggSupport: true,
        swfPath: ""
    })
	.jPlayer("onProgressChange", function (loadPercent, playedPercentRelative, playedPercentAbsolute, playedTime, totalTime) {
	    var playedTimeText = $.jPlayer.convertTime(playedTime),
            totalTimeText = $.jPlayer.convertTime(totalTime);
	    if (playedTimeText != jpPlayTime_LastText)
	        jpPlayTime.text(jpPlayTime_LastText = playedTimeText);
	    if (totalTimeText != jpTotalTime_LastText)
	        jpTotalTime.text(jpTotalTime_LastText = totalTimeText);

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
    }, false);

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
        loadPlaylistSync({ list: [], name: null });
    }

    function loadPlaylistSync(listdata, suppressPlay) {
        playlistElem.empty();
        var list = listdata.list;
        for (i = 0; i < list.length; i++) addToPlaylistRaw(list[i]);
        if (list.length > 0) playlistChange($("#jplayer_playlist ul li")[0], suppressPlay);
        else $("#jquery_jplayer").jPlayer("stop");
        playlistStorage.playlistLoaded(listdata);
        simStateSet.getting();
    }

    function playlistRefreshUi() {
        playlistStorage.contentsChanged(savePlaylist_ExtractFromHtml());
        simStateSet.getting();
    }

    function loadPlaylistAsync(listdata, suppressPlay) {
        function oops(ignore, errorstatus, errormessage) {
            alert(errorstatus + " while bouncing playlist off server\n" + (errormessage || ""));
        };

        $.ajax({
            type: "POST",
            url: "bounce-playlist",
            data: { playlist: JSON.stringify(listdata.list), format: "json" },
            timeout: 62 * 1000,
            success: function (data) { if (data && data.error) oops(null, data.error, data.message); else { listdata.list = data; loadPlaylistSync(listdata, suppressPlay); } },
            error: oops
        });
    }


    function savePlaylist_ExtractFromHtml() {
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


    var isGetQueued = false;
    var similarColSel = $("#similar"), perfMsgSel = $("#msgPerf"), knownSel = $("#similarKnown"), unknownSel = $("#similarUnknown");

    function updateSimilarDisplay(data) {
        var known = data.known, unknown = data.unknown;
        unknownSel.empty();
        for (var i = 0; i < unknown.length; i++)
            unknownSel.append($(document.createElement("li")).text(unknown[i]));
        knownSel.empty();
        similarColSel.removeClass("waiting");
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
                similarColSel.removeClass("proc-error");
                similarColSel.removeAttr("data-errname");
                similarColSel.removeAttr("data-errtrace");
                similarColSel.removeAttr("data-errmsg");
                similarColSel.addClass("processing");
                getSimilarImpl();
            }
        },
        done: function (data) {
            if (!data || data.error || !data.known || !data.unknown) { this.error("data-error", false, data); }
            else {
                if (mouseInSimilar) { simDispDataWait = data; similarColSel.addClass("waiting"); }
                else updateSimilarDisplay(data);
                perfMsgSel.attr("data-lookups", data.lookups);
                perfMsgSel.attr("data-weblookups", data.weblookups);
                perfMsgSel.attr("data-lookuperrs", data.lookuperrs);
                perfMsgSel.attr("data-milliseconds", data.milliseconds);
                perfMsgSel.attr("data-simdb-ms", data.msSimDb);

                if (simStateSet.current == "done") return;
                simStateSet.current = "done";
                similarColSel.removeClass("proc-error");
                similarColSel.removeAttr("data-errname");
                similarColSel.removeAttr("data-errtrace");
                similarColSel.removeAttr("data-errmsg");
                similarColSel.removeClass("processing");
                if (isGetQueued)
                    simStateSet.getting();
            }
        },
        error: function (status, retry, errordetail) {
            dataStatus = status || dataStatus;
            isGetQueued = isGetQueued || retry;
            if (errordetail) {
                similarColSel.attr("data-errmsg", errordetail.message);
                similarColSel.attr("data-errtrace", errordetail.fulltrace);
                similarColSel.attr("data-errname", errordetail.error);
            } else similarColSel.attr("data-errname", dataStatus);

            if (simStateSet.current == "error") return;
            simStateSet.current = "error";
            similarColSel.addClass("proc-error");
            similarColSel.removeClass("processing");
            perfMsgSel.removeAttr("data-lookups");
            if (isGetQueued)
                simStateSet.getting();
        },
        current: "done"
    }

    var simDispDataWait = null, dataStatus = null;

    var mouseInSimilar = false;
    knownSel.mouseenter(function (e) { mouseInSimilar = true; });
    knownSel.add(similarColSel).mouseleave(function (e) {
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
            data: { playlist: JSON.stringify(savePlaylist_ExtractFromHtml()) },
            timeout: 60*1000,
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

    function playlistChange(newCurrentItem, suppressPlay) {
        if (newCurrentItem != playlistItem)
            playlistConfig(newCurrentItem);
        if (playlistItem && !suppressPlay)
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
        var artist = clickedRowJQ.attr("data-artist");
        var title = clickedRowJQ.attr("data-title");
        addToPlaylist(
            { label: artist && title ? null : clickedRowJQ.attr("data-label") || GetFileName(clickedRowJQ.attr("data-href")),
                artist: clickedRowJQ.attr("data-artist"),
                title: clickedRowJQ.attr("data-title"),
                href: clickedRowJQ.attr("data-href"),
                length: parseInt(clickedRowJQ.attr("data-length")),
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

    $("#savePlaylistForm").submit(function savePlaylistAsM3u() { //TODO?
        $("#savePlaylistHiddenJson").val(JSON.stringify(savePlaylist_ExtractFromHtml()));
    });

    //////////////////////////////////////////////////////////////////////////////////////////////////DROP PLAYLIST


    window.globalDropHandler = function (e) {
        e.stopPropagation();
        e.preventDefault();
        var files = e.dataTransfer.files;
        if (!files.length) return true;
        var file = files[0];
        var filename = file.name;
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
                var dotIdx = filename.lastIndexOf(".");
                var filenameNoExtension = dotIdx != -1 ? filename.substring(0, dotIdx) : filename;


                if (jsResponse.error)
                    oops(null, jsResponse.error, jsResponse.message);
                else if (jsResponse)
                    loadPlaylistSync({ list: jsResponse, name: filenameNoExtension });
            }
        }

        var xhr = new XMLHttpRequest();

        var fd = new FormData();
        fd.append("format", "json");
        fd.append("playlist", file);
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
            dt.setData("application/x-song", JSON.stringify({ song: songdata, position: draggedItem.prevAll().length }));
            dt.setData("text/uri-list", songdata.href);
            dt.setData("Text", JSON.stringify({ song: songdata, position: draggedItem.prevAll().length })); //workaround for chrome...
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
            var targetPlaylistLI = $(target).parents().andSelf().filter("li").first();
            var sourcePlaylistLI = playlistElem.children().eq(draggedRawData.position);
            if (targetPlaylistLI[0] && targetPlaylistLI[0] === sourcePlaylistLI[0]) return true;


            var isDraggedFromThisBrowser = sourcePlaylistLI.length && sourcePlaylistLI.data("songdata").href == draggedData.href;
            var isDroppedOnPlaylistLI = targetPlaylistLI.length && targetPlaylistLI.data("songdata");

            var putSourceBeforeTarget = e.offsetY < (isDroppedOnPlaylistLI ?
                targetPlaylistLI[0].offsetTop + targetPlaylistLI[0].clientHeight / 2 :
                playlistElem[0].offsetTop + playlistElem[0].clientHeight / 2);

            if (isDraggedFromThisBrowser) sourcePlaylistLI.detach();
            var playlistLIToInsert = isDraggedFromThisBrowser ? sourcePlaylistLI : makeListItem(draggedData);

            if (isDroppedOnPlaylistLI) {
                if (putSourceBeforeTarget)
                    playlistLIToInsert.insertBefore(targetPlaylistLI);
                else
                    playlistLIToInsert.insertAfter(targetPlaylistLI);
            } else {
                if (putSourceBeforeTarget)
                    playlistElem.prepend(playlistLIToInsert); //dropped on UL.
                else
                    playlistElem.append(playlistLIToInsert); //dropped on UL.
            }

            playlistRefreshUi();
        });
    }

    /////////////////////////////////////////////////////////////////////////////////////Playlist Storage Handling
    var playlistStorage = function () {
        var public = {
            contentsChanged: function (listdata) {
                cmdQ.playlistContentsChangedTo = listdata;
                ProcessPlaylistQueue();
            },
            playlistLoaded: function (listdata) {
                cmdQ.playlistLoadedAs = listdata;
                ProcessPlaylistQueue();
            },
            loadPlaylistByID: function (playlistID) {
                cmdQ.playlistToLoad = playlistID;
                ProcessPlaylistQueue();
            },
            changePlaylistName: function (newName) {
                cmdQ.nameChangedTo = newName;
                ProcessPlaylistQueue();
            },
            loadLastList: function () {
                var lastList = JSON.parse(localStorage.playlist);
                var listName = window.location.hash.match(/^\#\<.+\>$/) && window.location.hash.slice(2, -1) || lastList && lastList.name
                if (!lastList && !listName)
                    return;
                else if (listName) {
                    if (state.currentPlaylists) {
                        loadLastListFromCurrentPlaylists(listName);
                    } else {
                        cmdQ.lastListToLoad = listName;
                    }
                }
                else if (!lastList.list)
                    loadPlaylistAsync({ list: lastList, name: null, PlaylistID: null }, true);
                else
                    loadPlaylistAsync(lastList, true);
            }
        };

        var cmdQ = {
            isBusy: false,
            playlistToLoad: null,
            nameChangedTo: null,
            playlistContentsChangedTo: null,
            playlistLoadedAs: null,
            lastListToLoad: null
        };

        var state = {
            playlistContents: null,
            playlistID: null,
            playlistName: null,
            currentPlaylists: null,
            changeNameFromData: function (newName) { state.playlistName = newName; $("#playlistName")[0].value = newName; window.songsearchSetPlaylistHashUri();  }
        };
        var playlistNamesUl = $("#playlistNames");
        window.songsearchSetPlaylistHashUri = function () {
            if (state.playlistName && window.location.hash.match(/^\#?(\<.+\>)?$/)) {
                window.location.hash = '#<' + state.playlistName + '>';
            }
        }


        function setBusy() { if (cmdQ.isBusy) throw "already busy!"; cmdQ.isBusy = true; }
        function setFree() { if (!cmdQ.isBusy) throw "not busy!"; cmdQ.isBusy = false; setTimeout(ProcessPlaylistQueue, 0); }
        function ProcessPlaylistQueue() {
            if (cmdQ.isBusy) return;

            if (cmdQ.playlistLoadedAs) {
                var listdata = cmdQ.playlistLoadedAs;
                cmdQ.playlistLoadedAs = null;
                var serializedList = JSON.stringify(listdata);
                localStorage.setItem("playlist", serializedList);
                userOptions.serializedList.setValue(serializedList);
                if (listdata.PlaylistID && listdata.PlaylistID === state.playlistID) {
                    if (!areEquivalent(listdata.list, state.playlistContents)) {
                        qUpdatePlaylistContents(listdata.name, listdata.list, listdata.PlaylistID);
                        return;
                    }
                } else if (listdata.PlaylistID) {
                    alert("Hmm, seems you've loaded " + listdata.name + "(" + listdata.PlaylistID + ") while " + state.playlistName + "(" + state.playlistID + ") is active??");
                } else if (!listdata.name) {
                    state.playlistContents = listdata.list;
                    state.playlistID = null;
                    state.changeNameFromData(null);
                } else {
                    qStorePlaylist(listdata.name, listdata.list);
                    state.changeNameFromData(listdata.name);
                    return;
                }
            }

            if (cmdQ.playlistContentsChangedTo) {
                var newplaylist = cmdQ.playlistContentsChangedTo;
                cmdQ.playlistContentsChangedTo = null;
                if (!areEquivalent(newplaylist, state.playlistContents)) {
                    var serializedList = JSON.stringify({ list: newplaylist, name: state.playlistName, PlaylistID: state.playlistID });
                    localStorage.setItem("playlist", serializedList);
                    userOptions.serializedList.setValue(serializedList);
                    if (state.playlistID === null) {
                        state.playlistContents = newplaylist;
                    } else { //playlist has changed, we're free, and we've got a current ID!
                        qUpdatePlaylistContents(state.playlistName, newplaylist, state.playlistID);
                        return;
                    }
                }
            }

            if (cmdQ.nameChangedTo !== null) {
                var newname = cmdQ.nameChangedTo;
                cmdQ.nameChangedTo = null;
                if (state.playlistName !== newname) {
                    if (state.playlistID !== null) { //we're free, name has changed, and pre-existing ID: rename!
                        qRenamePlaylist(state.playlistID, newname);
                        return;
                    } else if (newname !== "") {//we're free, name has changed, and no ID: save!
                        qStorePlaylist(newname, state.playlistContents);
                        return;
                    } //TODO: deal with empty name how?
                }
            }

            if (cmdQ.playlistToLoad) {
                qLoadPlaylist(cmdQ.playlistToLoad);
                cmdQ.playlistToLoad = null;
                return;
            }
        }


        function qRenamePlaylist(_playlistID, _newName) {
            setBusy();
            $.ajax({
                type: "POST",
                url: "rename-playlist",
                data: { playlistID: _playlistID, newName: _newName },
                timeout: 60000,
                success: function (data) {
                    try {
                        state.playlistName = _newName;
                        ajaxLoadPlaylistNames();
                    } finally {
                        setFree();
                    }
                },
                error: function (xhr, status, errorThrown) { alert(errorThrown); setFree(); }
            });
        }

        function qStorePlaylist(_playlistTitle, _playlistContents) {
            setBusy();
            $.ajax({
                type: "POST",
                url: "store-playlist",
                data: { playlistTitle: _playlistTitle, playlistContents: JSON.stringify(_playlistContents) },
                timeout: 60000,
                success: function (data) {
                    try {
                        state.playlistID = data;
                        state.playlistContents = _playlistContents;
                        ajaxLoadPlaylistNames();
                    } finally {
                        setFree();
                    }
                },
                error: function (xhr, status, errorThrown) { alert(errorThrown); setFree(); }
            });
        }

        function qUpdatePlaylistContents(_playlistTitle, _playlistContents, _lastVersionId) {
            setBusy();
            $.ajax({
                type: "POST",
                url: "update-playlist",
                data: { playlistTitle: _playlistTitle, playlistContents: JSON.stringify(_playlistContents), lastVersionId: _lastVersionId },
                timeout: 60000,
                success: function (data) {
                    try {
                        state.playlistID = data;
                        state.playlistContents = _playlistContents;
                        state.playlistName = _playlistTitle;
                        ajaxLoadPlaylistNames();
                    } finally {
                        setFree();
                    }
                },
                error: function (xhr, status, errorThrown) { alert(errorThrown); setFree(); }
            });
        }

        function qLoadPlaylist(playlistID) {
            setBusy();
            $.ajax({
                type: "POST",
                url: "load-playlist",
                data: { playlistID: playlistID },
                timeout: 60000,
                success: function (data) {
                    try {
                        state.playlistContents = JSON.parse(data.PlaylistContents);
                        state.playlistID = data.PlaylistID;
                        state.changeNameFromData(data.PlaylistTitle);
                        loadPlaylistSync({ list: state.playlistContents, name: data.PlaylistTitle, PlaylistID: data.PlaylistID });
                    } finally {
                        setFree();
                    }
                },
                error: function (xhr, status, errorThrown) { alert(errorThrown); setFree(); }
            });
        }

        var showOwnListsCheckbox = $("#showOnlyOwnLists");
        function ajaxLoadPlaylistNames() {
            $.ajax({
                type: "POST",
                url: showOwnListsCheckbox.prop("checked") ? "list-user-playlists" : "list-all-playlists",
                data: {},
                timeout: 60000,
                success: function (data) {
                    state.currentPlaylists = data;
                    playlistNamesUl.empty();
                    for (var i = 0; i < data.length; i++) {
                        playlistNamesUl.append(
                            $(document.createElement("li"))
                                .text(data[i].PlaylistTitle)
                                .attr("data-Username", data[i].Username)
                                .attr("data-CumulativePlayCount", data[i].CumulativePlayCount)
                                .attr("data-StoredTimestamp", data[i].StoredTimestamp)
                                .data("playlist", data[i])
                        );
                    }
                    if (cmdQ.lastListToLoad) {
                        loadLastListFromCurrentPlaylists(cmdQ.lastListToLoad);
                        cmdQ.lastListToLoad = null;
                    }
                },
                error: function (xhr, status, errorThrown) { alert(errorThrown); }
            });
        }
        function loadLastListFromCurrentPlaylists(lastListToLoadName) {
            var bestMatch = state.currentPlaylists.filter(function (playlist) {
                return playlist.PlaylistTitle.toLowerCase() === lastListToLoadName.toLowerCase();
            })[0];
            if (bestMatch) {
                public.loadPlaylistByID(bestMatch.PlaylistID);
            }
        }


        function playlistNameChangedHandler() { playlistStorage.changePlaylistName($("#playlistName")[0].value); }
        $("#playlistName").keyup(playlistNameChangedHandler).change(playlistNameChangedHandler).blur(playlistNameChangedHandler);
        playlistNamesUl.click(function (e) {
            if (!e) var e = window.event;
            var target = e.target || e.srcElement;
            var clickedListItem = $(target).parents().andSelf().filter("li").first();
            var plData = clickedListItem.data("playlist");
            playlistStorage.loadPlaylistByID(plData.PlaylistID);
        });

        setInterval(ajaxLoadPlaylistNames, 120000);
        showOwnListsCheckbox.change(ajaxLoadPlaylistNames);
        ajaxLoadPlaylistNames();
        return public;
    } ();
});

