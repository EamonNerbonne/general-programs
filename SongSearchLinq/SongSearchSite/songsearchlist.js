
$(document).ready(function ($) {
    function GetExtension(url) {
        var qIdx = url.indexOf("?");
        if (qIdx != -1) url = url.substring(0, qIdx);
        var hIdx = url.indexOf("#");
        if (hIdx != -1) url = url.substring(0, hIdx);
        url = decodeURIComponent(url);
        dotIdx = url.lastIndexOf(".");
        if (dotIdx == -1) return null;
        else return url.substring(dotIdx);
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
        saver: {
            label: "Save",
            type: "textbox",
            initialValue: ""
        },
        loader: {
            label: "Load",
            type: "textbox",
            initialValue: "",
            onchange: function (newval, e) {
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
            onchange: function (newval, e) {
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
        userOptions.saver.element.focus(function () {
            userOptions.saver.setValue(JSON.stringify(savePlaylist()));
            userOptions.saver.element.select();
        });
        userOptions.loader.element.focus(function () {
            userOptions.loader.element.select();
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
        else playListChange(clickedListItem);
    }


    var playListElem = null;

    $("#jquery_jplayer").jPlayer({
        ready: function () {
            playListElem = $(document.createElement("ul"))
                    .appendTo($("#jplayer_playlist").empty())
                    .click(playlistClick)
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
        playListElem.sortable("refresh");
    }

    function loadPlaylist(list) {
        emptyPlaylist();
        for (i = 0; i < list.length; i++)
            addToPlaylistRaw(list[i]);
        playlistRefreshUi();
    }
    function savePlaylist() {
        return $("#jplayer_playlist ul li").map(function (i, e) { return $(e).data("songdata"); }).get();
    }

    function makeListItem(song) {
        return $(document.createElement("li")).text(song.label).data("songdata", song).append(
            $(document.createElement("div")).text("x").addClass("deleteButton")
        );
    }

    function addToPlaylist(song) {
        var shouldStart = playListElem.children().length == 0;
        addToPlaylistRaw(song);
        if (shouldStart) playListChange($("#jplayer_playlist ul li")[0]);
        playlistRefreshUi();
    }

    function playlistRefreshUi() {
        playListElem.sortable("refresh");
        userOptions.saver.setValue(JSON.stringify(savePlaylist()));
        getSimilar();
    }

    function getSimilar() {
        $("#similar").addClass("processing");
        $.post("similar-to", { playlist: JSON.stringify(savePlaylist()) }, function (data) {
            var known = data.known, unknown = data.unknown;
            var knownEl = $("#similar .known").empty(), unknownEl = $("#similar .unknown").empty();
            for (var i = 0; i < unknown.length; i++) {
                unknownEl.append($(document.createElement("li")).text(unknown[i]));
            }
            for (var i = 0; i < known.length; i++) {
                $(document.createElement("li")).text(known[i].label).data("songdata", known[i]).appendTo(knownEl);
            }
            $("#similar").removeClass("processing");
        });
    }

    function addToPlaylistRaw(song) {
        var listItem = makeListItem(song);
        listItem.appendTo(playListElem);
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
        }
    }

    function playListChange(listItem) {
        if (listItem != playListItem)
            playListConfig(listItem);
        if (playListItem)
            $("#jquery_jplayer").jPlayer("play");
    }

    function playListDelete(listItem) {
        if (listItem == playListItem)
            playListChange(null);
        $(listItem).remove();
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
        addToPlaylist({ label: clickedRow.attr("data-label"), href: clickedRow.attr("data-href"), length: clickedRow.attr("data-length") });
    };
});

