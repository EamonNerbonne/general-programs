
$(document).ready(function () {
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

    // Local copy of jQuery selectors, for performance.
    var jpPlayTime = $("#jplayer_play_time");
    var jpTotalTime = $("#jplayer_total_time");

    $("#jquery_jplayer").jPlayer({
        ready: function () {
            displayPlayList();
        },
        oggSupport: true,
        swfPath: ""
    })
	.jPlayer("onProgressChange", function (loadPercent, playedPercentRelative, playedPercentAbsolute, playedTime, totalTime) {
	    jpPlayTime.text($.jPlayer.convertTime(playedTime));
	    jpTotalTime.text($.jPlayer.convertTime(totalTime));
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

    function playlistClick(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedListItem = $(target).parents().andSelf().filter("li").first()[0];

        if (clickedListItem != playListItem)
            playListChange(clickedListItem);
        else
            $("#jquery_jplayer").jPlayer("play");

    }
    function makeListItem(song) { return $(document.createElement("li")).text(song.name).data("songdata", song); }
    function addToPlaylist(song) {
        var listEl = $("#jplayer_playlist ol");
        var listItem = makeListItem(song).appendTo(listEl);
        if (listEl.children().length == 1)
            playListChange(listItem[0]);
    }
    function displayPlayList() { $("#jplayer_playlist").empty().append($(document.createElement("ol")).click(playlistClick)); }

    function playListConfig(listItem) {
        if (playListItem)
            $(playListItem).removeClass("jplayer_playlist_current");
        playListItem = listItem;
        if (playListItem) {
            $(playListItem).addClass("jplayer_playlist_current");
            $("#jquery_jplayer").jPlayer("loadSong", $(playListItem).data("songdata").uris);
        }
    }

    function playListChange(listItem) {
        playListConfig(listItem);
        if (playListItem)
            $("#jquery_jplayer").jPlayer("play");
    }

    function playListNext() { playListChange($(playListItem).next()[0]); }

    function playListPrev() { playListChange($(playListItem).prev()[0]); }

    function SearchListClicked_impl(e) {
        if (!e) var e = window.event;
        var target = e.target || e.srcElement;
        var clickedRow = $(target).parents("tr");
        if (clickedRow.length != 1)
            return;
        var songUri = clickedRow.attr("data-href");
        var songLabel = clickedRow.attr("data-songlabel");
        var songType = GuessMime(GetExtension(songUri));
        addToPlaylist({ name: songLabel, uris: [{ type: songType, src: songUri}] });
    }
    window.SearchListClicked = SearchListClicked_impl;
});

