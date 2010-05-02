
$(document).ready(function ($) {
    function GetExtension(url) {
        dotIdx = url.lastIndexOf(".");
        if (dotIdx == -1) return null;
        else return url.substring(dotIdx);
    }

    function GuessMime(extension) {
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

    var playListItem = null;

    var useNotifications = window.webkitNotifications && window.webkitNotifications.checkPermission() == 0;
    var hasBeenNotified = false;
    var userOptions = {};

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
    if (!$.isEmptyObject(userOptions))
        $("#optionsBox").OptionsBuilder(userOptions);


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

    function makeListItem(song) {
        return $(document.createElement("li")).text(song.name).data("songdata", song).append(
            $(document.createElement("div")).text("x").addClass("deleteButton")
        );
    }

    function addToPlaylist(song) {
        var listItem = makeListItem(song);
        listItem.appendTo(playListElem);
        playListElem.sortable("refresh");
        if (playListElem.children().length == 1)
            playListChange(listItem[0]);
    }

    function playListConfig(listItem) {
        if (playListItem)
            $(playListItem).removeClass("jplayer_playlist_current");
        playListItem = listItem;
        if (playListItem) {
            $(playListItem).addClass("jplayer_playlist_current");
            hasBeenNotified = false;
            $("#jquery_jplayer").jPlayer("loadSong", $(playListItem).data("songdata").uris);
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

    window.SearchListClicked = function SearchListClicked_impl(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedRow = $(target).parents("tr");
        if (clickedRow.length != 1)
            return;
        var songUri = clickedRow.attr("data-href");
        var songLabel = clickedRow.attr("data-songlabel");
        var songType = GuessMime(GetExtension(songUri));
        addToPlaylist({ name: songLabel, uris: [{ type: songType, src: songUri}] });
    };
});

