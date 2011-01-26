
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
        , lfmUser: {
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

        if (clickedDelete)
            playlistDelete(clickedListItem);
        else if (e.type == "dblclick") {
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



    var playlistContainer = $("#jplayer_playlist");
    var playlistElem = null;

    $("#jquery_jplayer").jPlayer({
        ready: function () {
            playlistElem = $(document.createElement("ul"))
                    .appendTo(playlistContainer.empty())
                    .click(playlistClick).dblclick(playlistClick)
                    .sortable().disableSelection();
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
	        else {
	            var songTitle = $(playlistItem).contents(":empty").text();
	            var popup = window.webkitNotifications.createNotification("emnicon.png", songTitle, songTitle);
	            popup.ondisplay = function () { setTimeout(function () { popup.cancel(); }, 5000); }
	            popup.show();
	        }
	    }
	})
	.jPlayer("onSoundComplete", function () {
	    playlistNext();
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

    function loadPlaylist(list) {
        playlistElem.empty();
        for (i = 0; i < list.length; i++)
            addToPlaylistRaw(list[i]);
        if (list.length > 0) playlistChange($("#jplayer_playlist ul li")[0]);
        else $("#jquery_jplayer").jPlayer("stop");
        playlistRefreshUi();
    }
    function savePlaylist() {
        return $("#jplayer_playlist ul li").map(function (i, e) { return $(e).data("songdata"); }).get();
    }

    function makeListItem(song) {
        return $(document.createElement("li")).text(song.label).data("songdata", song).append(
            $(document.createElement("div")).text("x").addClass("deleteButton")
        ).disableSelection();
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

    function playlistRefreshUi() {
        playlistElem.sortable("refresh");
        var serializedList = JSON.stringify(savePlaylist());
        userOptions.serializedList.setValue(serializedList);
        localStorage.setItem("playlist", serializedList);               // defining the localStorage variable 
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
        for (var i = 0; i < known.length; i++)
            knownSel.append($(document.createElement("li")).text(known[i].label).data("songdata", known[i]));
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

    function playlistNext() { playlistChange($(playlistItem).next()[0]); }

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
                href: clickedRowJQ.attr("data-href"),
                length: clickedRowJQ.attr("data-length"),
                replaygain: Number(clickedRowJQ.attr("data-replaygain")) || 0
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
        updateResultsGlobal();
    };
});

