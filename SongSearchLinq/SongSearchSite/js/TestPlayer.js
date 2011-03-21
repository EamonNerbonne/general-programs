
$(document).ready(function ($) {

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
            fill_list();
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
	            var popup = window.webkitNotifications.createNotification("img/emnicon.png", songTitle, songTitle);
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
            var song = $(playListItem).data("songdata");
            if(song.uris)
                $("#jquery_jplayer").jPlayer("loadSong", song.uris);
            else
                $("#jquery_jplayer").jPlayer("setFile", song.mp3, song.ogg);
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


    function fill_list() {
        var myPlayListNew = [
    		{ name: "Tempered Song (old-style)", mp3: "http://www.miaowmusic.com/mp3/Miaow-01-Tempered-song.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-01-Tempered-song.ogg" },
    		{ name: "Hidden (new; mp3>ogg)", uris: [{ type: "audio/mpeg", src: "http://www.miaowmusic.com/mp3/Miaow-02-Hidden.mp3" }, { type: "application/ogg", src: "http://www.miaowmusic.com/ogg/Miaow-02-Hidden.ogg"}] },
    		{ name: "Lentement (new; mp3 only)", uris: [{ type: "audio/mpeg", src: "http://www.miaowmusic.com/mp3/Miaow-03-Lentement.mp3"}] },
    		{ name: "Lismore (new; ogg onlye)", uris: [{ type: "audio/ogg", src: "http://www.miaowmusic.com/ogg/Miaow-04-Lismore.ogg"}] },
    		{ name: "The Separation (new; mp3<ogg)", uris: [{ type: "audio/ogg", src: "http://www.miaowmusic.com/ogg/Miaow-05-The-separation.ogg" }, { type: "audio/mpeg", src: "http://www.miaowmusic.com/mp3/Miaow-05-The-separation.mp3"}] },
    		{ name: "Beside Me", mp3: "http://www.miaowmusic.com/mp3/Miaow-06-Beside-me.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-06-Beside-me.ogg" },
    		{ name: "Bubble", mp3: "http://www.miaowmusic.com/mp3/Miaow-07-Bubble.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-07-Bubble.ogg" },
    		{ name: "Stirring of a Fool", mp3: "http://www.miaowmusic.com/mp3/Miaow-08-Stirring-of-a-fool.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-08-Stirring-of-a-fool.ogg" },
    		{ name: "Partir", mp3: "http://www.miaowmusic.com/mp3/Miaow-09-Partir.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-09-Partir.ogg" },
    		{ name: "Thin Ice", mp3: "http://www.miaowmusic.com/mp3/Miaow-10-Thin-ice.mp3", ogg: "http://www.miaowmusic.com/ogg/Miaow-10-Thin-ice.ogg" }
    	];


        for (var i = 0; i < myPlayListNew.length; i++) {
            addToPlaylist(myPlayListNew[i]);
        }
    }
});

