
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

    var playListItem = null;

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
            playListDelete(clickedListItem);
        else if (e.type == "dblclick") {
            if (document.selection && document.selection.empty)
                document.selection.empty();
            else if (window.getSelection) {
                var selection = window.getSelection();
                if (selection && selection.removeAllRanges)
                    selection.removeAllRanges();
            }
            playListChange(clickedListItem);
        }
    }



    var playlistContainer = $("#jplayer_playlist");
    var playListElem = null;

    $("#jquery_jplayer").jPlayer({
        ready: function () {
            playListElem = $(document.createElement("ul"))
                    .appendTo(playlistContainer.empty())
                    .click(playlistClick).dblclick(playlistClick)
                    .sortable().disableSelection();
            $("#similar .known").click(knownClick);
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
	            var songTitle = $(playListItem).contents(":empty").text();
	            var popup = window.webkitNotifications.createNotification("emnicon.png", songTitle, songTitle);
	            popup.ondisplay = function () { setTimeout(function () { popup.cancel(); }, 5000); }
	            popup.show();
	        }
	    }
	})
	.jPlayer("onSoundComplete", function () {
	    playListNext();
	});

    $("#jplayer_previous").click(function () {
        playListPrev();
        return false;
    });

    $("#jplayer_next").click(function () {
        playListNext();
        return false;
    });

    function emptyPlaylist() {
        playListChange(null);
        playListElem.empty();
        playlistRefreshUi();
    }

    function loadPlaylist(list) {
        playListElem.empty();
        for (i = 0; i < list.length; i++)
            addToPlaylistRaw(list[i]);
        if (list.length > 0) playListChange($("#jplayer_playlist ul li")[0]);
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
        listItem.appendTo(playListElem);
        return listItem[0];
    }


    function addToPlaylist(song) {
        var shouldStart = playListElem.children().length == 0;
        var newItem = addToPlaylistRaw(song);
        playlistRefreshUi();
        if (shouldStart) playListChange(newItem);
        else scrollIntoMiddleView(newItem);
    }

    function playlistRefreshUi() {
        playListElem.sortable("refresh");
        userOptions.serializedList.setValue(JSON.stringify(savePlaylist()));
        simStateSet.getting();
    }

    var isGetQueued = false;
    var leftColSel = $(".similarItemsDisplay"), knownSel = $("#similar .known"), unknownSel = $("#similar .unknown");

    var simStateSet = {
        getting: function () {
            if (simStateSet.current == "getting")
                isGetQueued = true;
            else {
                isGetQueued = false;
                simStateSet.current = "getting";
                leftColSel.removeClass("proc-error");
                leftColSel.addClass("processing");
                getSimilarImpl();
            }
        },
        done: function () {
            if (simStateSet.current == "done") return;
            simStateSet.current = "done";
            leftColSel.removeClass("proc-error");
            leftColSel.removeClass("processing");
            if (isGetQueued)
                simStateSet.getting();
        },
        error: function () {
            if (simStateSet.current == "error") return;
            simStateSet.current = "error";
            leftColSel.addClass("proc-error");
            leftColSel.removeClass("processing");
            if (isGetQueued)
                simStateSet.getting();
        },
        current: "done"
    }

    var simDispDataWait = null;

    var mouseInSimilar = false;
    knownSel.hover(function (e) { mouseInSimilar = true; }, function (e) {
        mouseInSimilar = false;
        if (simDispDataWait) {
            updateSimilarDisplay(simDispDataWait);
            simDispDataWait = null;
        }
    });

    function updateSimilarDisplay(data) {
        var known = data.known, unknown = data.unknown;
        unknownSel.empty();
        for (var i = 0; i < unknown.length; i++)
            unknownSel.append($(document.createElement("li")).text(unknown[i]));
        knownSel.empty().removeClass("waiting");
        for (var i = 0; i < known.length; i++)
            knownSel.append($(document.createElement("li")).text(known[i].label).data("songdata", known[i]));
    }

    function getSimilarImpl() {
        $.ajax({
            type: "POST",
            url: "similar-to",
            data: { playlist: JSON.stringify(savePlaylist()) },
            timeout: 10000,
            success: function (data) {
                if (mouseInSimilar) {
                    simDispDataWait = data;
                    knownSel.addClass("waiting")
                } else
                    updateSimilarDisplay(data);
                simStateSet.done();
            },
            error: function (xhr, status, errorThrown) {
                if (status == "timeout")
                    isGetQueued = true;
                simStateSet.error();
            }
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

    function playListConfig(listItem) {
        if (playListItem)
            $(playListItem).removeClass("jplayer_playlist_current");
        playListItem = listItem;
        if (playListItem) {
            $(playListItem).addClass("jplayer_playlist_current");
            hasBeenNotified = false;
            var songHref = $(playListItem).data("songdata").href;
            $("#jquery_jplayer").jPlayer("loadSong", [{ type: UriToMime(songHref), src: songHref}]);
            scrollIntoMiddleView(playListItem);
        }
    }



    function playListChange(listItem) {
        if (listItem != playListItem)
            playListConfig(listItem);
        if (playListItem)
            $("#jquery_jplayer").jPlayer("play");
        else
            $("#jquery_jplayer").jPlayer("stop");
    }

    function playListDelete(listItem) {
        if (listItem == playListItem)
            playListChange(null);
        $(listItem).remove();
        playlistRefreshUi();
    }

    function playListNext() { playListChange($(playListItem).next()[0]); }

    function playListPrev() { playListChange($(playListItem).prev()[0]); }

    function knownClick(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedListItem = $(target).parents().andSelf().filter("li").first();
        addToPlaylist(clickedListItem.data("songdata"));
    }

    window.SearchListClicked = function SearchListClicked_impl(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedRow = $(target).parents("tr");
        if (clickedRow.length != 1)
            return;
        addToPlaylist({ label: clickedRow.attr("data-label") || GetFileName(clickedRow.attr("data-href")), href: clickedRow.attr("data-href"), length: clickedRow.attr("data-length") });
    };
});

