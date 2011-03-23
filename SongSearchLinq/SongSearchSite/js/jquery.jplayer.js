/*
* Copyright (c) 2010 Eamon Nerbonne
* Dual licensed under the MIT and GPL licenses.
*  - http://www.opensource.org/licenses/mit-license.php
*  - http://www.gnu.org/copyleft/gpl.html
*
* Author: Eamon Nerbonne
* Date: 2010-04-30
*
* Based on:
*    jPlayer Plugin for jQuery JavaScript Library
*    http://www.happyworm.com/jquery/jplayer
*    Author: Mark J Panaghiston
*    Version: 1.1.0
*    Date: 26th March 2010
*/

(function ($) {
    function objKeys(obj) {
        var keys = [];
        for (var key in obj)
            keys.push(key);
    }
    function objAll(obj, test) {
        for (var key in obj)
            if (!test(obj[key], key))
                return false;
        return true;
    }
    function passToHandler(func) {
        return function () {
            if (this.handlers.current)
                func.apply(this, $.merge([this.handlers[this.handlers.current]], arguments));
        };
    }
    function trycatch(func, onerr) {
        return function () {
            try {
                return func.apply(this, arguments);
            } catch (err) { return onerr(err); }
        };
    }

    function limitValue(value, min, max) { return Math.min(Math.max(min, value), max); }

    var validGetters = ["jPlayerOnProgressChange", "jPlayerOnSoundComplete", "jPlayerVolume", "jPlayerVolumeRaw", "jPlayerReady", "getData", "jPlayerController"];
    // Adapted from ui.core.js (1.7.2) $.widget() "create plugin method"
    // $.data() info at http://docs.jquery.com/Internals/jQuery.data
    $.fn.jPlayer = function (member) {

        var pluginName = "jPlayer";
        var isMethodCall = (typeof member == 'string');
        var args = Array.prototype.slice.call(arguments, 1);

        // prevent calls to internal methods
        if (isMethodCall && member.substring(0, 1) == '_') {
            return this;
        }
        var isGetter = isMethodCall && -1 != $.inArray(member, validGetters);

        // handle getter methods
        if (isGetter) {
            var instance = $.data(this[0], pluginName);
            return instance && instance[member].apply(instance, args);
        }

        // handle initialization and non-getter methods
        return this.each(function () {
            var instance = $.data(this, pluginName);

            // constructor
            if (!instance && !isMethodCall) {
                $.data(this, pluginName, new $[pluginName](this, member))._init();
            }

            // method call
            instance && isMethodCall && $.isFunction(instance[member]) && instance[member].apply(instance, args);
        });
    };

    $.jPlayer = function (element, options) {
        this.options = $.extend({}, options);
        this.element = $(element);
    };

    $.jPlayer.defaults = {
        cssPrefix: "jqjp",
        swfPath: "js",
        volume: 80,
        oggSupport: false,
        nativeSupport: true,
        flashSupport: true,
        customCssIds: false,
        graphicsFix: true,
        errorAlerts: false,
        warningAlerts: false,
        position: "absolute",
        width: "0",
        height: "0",
        top: "0",
        left: "0",
        quality: "high",
        bgcolor: "#ffffff"
    };

    var _configOverride = {
        version: "1.1.0",
        swfVersionRequired: "1.1.0",
        swfVersion: "unknown",
        jPlayerControllerId: undefined,
        delayedCommandId: undefined,
        isWaitingForPlay: false,
        isFileSet: false
    };

    var _diagInit = {
        isPlaying: false,
        src: "",
        loadPercent: 0,
        playedPercentRelative: 0,
        playedPercentAbsolute: 0,
        playedTime: 0,
        totalTime: 0,
        gainScale: 0.3
    };

    var _cssIdDefaultsForActions = {
        play: "jplayer_play",
        pause: "jplayer_pause",
        stop: "jplayer_stop",
        loadBar: "jplayer_load_bar",
        playBar: "jplayer_play_bar",
        volumeMin: "jplayer_volume_min",
        volumeMax: "jplayer_volume_max",
        volumeBar: "jplayer_volume_bar",
        volumeBarValue: "jplayer_volume_bar_value"
    };

    var instanceCount = 0;

    $.jPlayer.timeFormat = {
        showHour: false,
        showMin: true,
        showSec: true,
        padHour: false,
        padMin: true,
        padSec: true,
        sepHour: ":",
        sepMin: ":",
        sepSec: ""
    };

    $.jPlayer.convertTime = function (mSec) {
        var myTime = new Date(mSec);
        var hour = myTime.getUTCHours();
        var min = myTime.getUTCMinutes();
        var sec = myTime.getUTCSeconds();
        var strHour = ($.jPlayer.timeFormat.padHour && hour < 10) ? "0" + hour : hour;
        var strMin = ($.jPlayer.timeFormat.padMin && min < 10) ? "0" + min : min;
        var strSec = ($.jPlayer.timeFormat.padSec && sec < 10) ? "0" + sec : sec;
        return (($.jPlayer.timeFormat.showHour) ? strHour + $.jPlayer.timeFormat.sepHour : "") + (($.jPlayer.timeFormat.showMin) ? strMin + $.jPlayer.timeFormat.sepMin : "") + (($.jPlayer.timeFormat.showSec) ? strSec + $.jPlayer.timeFormat.sepSec : "");
    };


    $.jPlayer.prototype = {
        _init: function () {
            var self = this;
            var element = this.element;

            this.config = $.extend({}, $.jPlayer.defaults, this.options, _configOverride);
            this.config.diag = $.extend({}, _diagInit);
            this.config.cssId = {};
            this.config.cssSelector = {};
            this.config.cssDisplay = {};
            this.config.clickHandler = {};

            this.element.data("jPlayer.config", this.config);

            $.extend(this.config, {
                id: this.element.attr("id"),
                swf: this.config.swfPath + ((this.config.swfPath != "" && this.config.swfPath.slice(-1) != "/") ? "/" : "") + "Jplayer.swf",
                fid: this.config.cssPrefix + "_flash_" + instanceCount,
                aid: this.config.cssPrefix + "_audio_" + instanceCount,
                hid: this.config.cssPrefix + "_force_" + instanceCount,
                i: instanceCount,
                volume: limitValue(this.config.volume, 0, 100)
            });

            instanceCount++;

            if (this.config.ready != undefined) {
                if ($.isFunction(this.config.ready)) {
                    this.jPlayerReadyCustom = this.config.ready;
                } else {
                    this._warning("Constructor's ready option is not a function.");
                }
            }

            try {
                this.config.audio = new Audio();
                this.config.audio.id = this.config.aid;
                this.element.append(this.config.audio);
            } catch (err) {
                this.config.audio = {};
            }

            var handlers = {
                current: false,
                setButtons: function (playing) {
                    self.config.diag.isPlaying = playing;
                    if (self.config.cssId.play != undefined && self.config.cssId.pause != undefined) {
                        if (playing) {
                            self.config.cssSelector.play.css("display", "none");
                            self.config.cssSelector.pause.css("display", self.config.cssDisplay.pause);
                        } else {
                            self.config.cssSelector.play.css("display", self.config.cssDisplay.play);
                            self.config.cssSelector.pause.css("display", "none");
                        }
                    }
                    if (playing) {
                        self.config.isWaitingForPlay = false;
                    }
                }
            };

            handlers.flash = {
                willPlayType: function (type) {
                    return self.config.flashSupport && type == "audio/mpeg" && self._checkForFlash(8);
                },
                loadSong: function (type, src, replaygain) {
                    self.config.diag.gainScale = Math.min(1.0, 0.5 * Math.pow(10, (replaygain || 0.0) / 20.0));
                    self._getMovie().fl_setFile_mp3(src);
                    self._getMovie().fl_volume_mp3(Math.sqrt(100 * self.config.volume * self.config.diag.gainScale));
                    self.config.diag.src = src;
                    self.config.isFileSet = true; // Set here for conformity, but the flash handles this internally and through return values.
                    self.handlers.setButtons(false);
                },
                clearFile: function () {
                    self._getMovie().fl_clearFile_mp3();
                    self.config.diag.src = "";
                    self.config.isFileSet = false;
                    self.handlers.setButtons(false);
                },
                play: function () {
                    if (self._getMovie().fl_play_mp3())
                        self.handlers.setButtons(true);
                },
                pause: function () {
                    if (self._getMovie().fl_pause_mp3())
                        self.handlers.setButtons(false);
                },
                stop: function () {
                    if (self._getMovie().fl_stop_mp3())
                        self.handlers.setButtons(false);
                },
                playHead: function (p) {
                    if (self._getMovie().fl_play_head_mp3(p))
                        self.handlers.setButtons(true);
                },
                playHeadTime: function (t) {
                    if (self._getMovie().fl_play_head_time_mp3(t))
                        self.handlers.setButtons(true);
                },
                volume: function (v) {
                    self.config.volume = v;
                    self._getMovie().fl_volume_mp3(Math.sqrt(100 * v * self.config.diag.gainScale));
                }
            };


            for (var handlerName in handlers.flash) handlers.flash[handlerName] = trycatch(handlers.flash[handlerName], self._flashError);

            handlers.html5 = {
                willPlayType: function (type) {
                    var canplay = self.config.nativeSupport && self.config.audio.canPlayType && self.config.audio.canPlayType(type);
                    return !!(canplay && canplay != "no");
                },
                loadSong: function (type, src, replaygain) {
                    self.config.diag.gainScale = Math.min(1.0, 0.5 * Math.pow(10, (replaygain || 0.0) / 20.0));

                    self.config.audio = new Audio();
                    self.config.audio.id = self.config.aid;
                    self.config.aSel.replaceWith(self.config.audio);
                    self.config.aSel = $("#" + self.config.aid);
                    self.config.diag.src = src;
                    self.config.isWaitingForPlay = true;
                    self.config.isFileSet = true;
                    self.jPlayerOnProgressChange(0, 0, 0, 0, 0);
                    clearInterval(self.config.jPlayerControllerId);
                    //self.config.audio.addEventListener("canplay", function () {
                        self.config.audio.volume = self.config.volume * self.config.diag.gainScale / 100; // Fix for Chrome 4: Event solves initial volume not being set correctly.
                    //}, false);
                    self.handlers.setButtons(false);
                },
                clearFile: function () {
                    this.loadSong("", "");
                    self.config.isWaitingForPlay = false;
                    self.config.isFileSet = false;
                },
                play: function () {
                    if (self.config.isFileSet) {
                        if (self.config.isWaitingForPlay)
                            self.config.audio.src = self.config.diag.src;
                        self.config.audio.play();
                        clearInterval(self.config.jPlayerControllerId);
                        self.config.jPlayerControllerId = window.setInterval(function () {
                            self.jPlayerController(false);
                        }, 100);
                        clearInterval(self.config.delayedCommandId);
                        self.handlers.setButtons(true);
                    }
                },
                pause: function () {
                    if (self.config.isFileSet) {
                        self.config.audio.pause();
                        self.handlers.setButtons(false);
                    }
                },
                stop: function () {
                    if (self.config.isFileSet) {
                        try {
                            self.config.audio.currentTime = 0;
                            this.pause();
                            clearInterval(self.config.jPlayerControllerId);
                            self.config.jPlayerControllerId = window.setInterval(function () {
                                self.jPlayerController(true); // With override true
                            }, 100);

                        } catch (err) {
                            clearInterval(self.config.delayedCommandId);
                            self.config.delayedCommandId = window.setTimeout(function () {
                                self.stop();
                            }, 100);
                        }
                    }
                },
                playHead: function (p) {
                    if (self.config.isFileSet) {
                        try {
                            if ((typeof self.config.audio.buffered == "object") && (self.config.audio.buffered.length > 0)) {
                                self.config.audio.currentTime = p * self.config.audio.buffered.end(self.config.audio.buffered.length - 1) / 100;
                            } else {
                                self.config.audio.currentTime = p * self.config.audio.duration / 100;
                            }
                            this.play();
                        } catch (err) {
                            clearInterval(self.config.delayedCommandId);
                            self.config.delayedCommandId = window.setTimeout(function () {
                                self.playHead(p);
                            }, 100);
                        }
                    }
                },
                playHeadTime: function (t) {
                    if (self.config.isFileSet) {
                        try {
                            self.config.audio.currentTime = t / 1000;
                            this.play();
                        } catch (err) {
                            clearInterval(self.config.delayedCommandId);
                            self.config.delayedCommandId = window.setTimeout(function () {
                                self.playHeadTime(t);
                            }, 100);
                        }
                    }
                },
                volume: function (v) {
                    self.config.volume = v;
                    self.config.audio.volume = v * self.config.diag.gainScale / 100;
                    self.jPlayerVolumeRaw(v * self.config.diag.gainScale);
                }
            };

            var audioTypes = ["audio/ogg", "application/ogg", "audio/mpeg"];
            var backends = ["html5", "flash" ]; //chrome's mp3 streaming support isn't too stellar; prefer flash.
            var support = {};
            $.each(audioTypes, function (i, audioType) {
                support[audioType] = $.grep(backends, function (backend, j) {
                    return handlers[backend].willPlayType(audioType);
                });
            });

            this.config.support = support;
            this.config.aSel = $("#" + this.config.aid);

            var usingBackends = {};
            $.each(
            //used backends are: all backends such that there exists an audioType such that for that audioType the backend is preferred.
                $.grep(backends, function (backend, i) { return $.grep(audioTypes, function (type, i) { return support[type][0] == backend; }).length > 0; }),
                function (i, backend) { usingBackends[backend] = 1; handlers.current = handlers.current || backend; }
            );

            this.config.usingBackends = usingBackends;

            self.handlers = handlers;

            if (this.config.usingBackends.flash) {
                if (this._checkForFlash(8)) {
                    if ($.browser.msie) {
                        var html_obj = '<object id="' + this.config.fid + '"';
                        html_obj += ' classid="clsid:d27cdb6e-ae6d-11cf-96b8-444553540000"';
                        html_obj += ' codebase="' + document.URL.substring(0, document.URL.indexOf(':')) + '://fpdownload.macromedia.com/pub/shockwave/cabs/flash/swflash.cab"'; // Fixed IE non secured element warning.
                        html_obj += ' type="application/x-shockwave-flash"';
                        html_obj += ' width="' + this.config.width + '" height="' + this.config.height + '">';
                        html_obj += '</object>';

                        var obj_param = new Array();
                        obj_param[0] = '<param name="movie" value="' + this.config.swf + '" />';
                        obj_param[1] = '<param name="quality" value="high" />';
                        obj_param[2] = '<param name="FlashVars" value="id=' + escape(this.config.id) + '&fid=' + escape(this.config.fid) + '&vol=' + Math.sqrt(100 * this.config.volume * self.config.diag.gainScale) + '" />';
                        obj_param[3] = '<param name="allowScriptAccess" value="always" />';
                        obj_param[4] = '<param name="bgcolor" value="' + this.config.bgcolor + '" />';

                        var ie_dom = document.createElement(html_obj);
                        for (var i = 0; i < obj_param.length; i++) {
                            ie_dom.appendChild(document.createElement(obj_param[i]));
                        }
                        this.element.append(ie_dom);
                    } else {
                        var html_embed = '<embed name="' + this.config.fid + '" id="' + this.config.fid + '" src="' + this.config.swf + '"';
                        html_embed += ' width="' + this.config.width + '" height="' + this.config.height + '" bgcolor="' + this.config.bgcolor + '"';
                        html_embed += ' quality="high" FlashVars="id=' + escape(this.config.id) + '&fid=' + escape(this.config.fid) + '&vol=' + Math.sqrt(100 * this.config.volume * self.config.diag.gainScale) + '"';
                        html_embed += ' allowScriptAccess="always"';
                        html_embed += ' type="application/x-shockwave-flash" pluginspage="http://www.macromedia.com/go/getflashplayer" />';
                        this.element.append(html_embed);
                    }

                } else {
                    this.element.html("<p>Flash 8 or above is not installed. <a href='http://get.adobe.com/flashplayer'>Get Flash!</a></p>");
                }
            }

            this.element.css({ 'position': this.config.position, 'top': this.config.top, 'left': this.config.left });

            if (this.config.graphicsFix) {
                var html_hidden = '<div id="' + this.config.hid + '"></div>';
                this.element.append(html_hidden);

                $.extend(this.config, {
                    hSel: $("#" + this.config.hid)
                });
                this.config.hSel.css({ 'text-indent': '-9999px' });
            }

            if (!this.config.customCssIds) {
                $.each(_cssIdDefaultsForActions, function (name, id) {
                    self.cssId(name, id);
                });
            }

            if (this.config.usingBackends.html5) { // Emulate initial flash call after 100ms
                this.element.css({ 'left': '-9999px' }); // Mobile Safari always shows the <audio> controls, so hide them.
                window.setTimeout(function () {
                    self._jPlayerReadyBackend("html5");
                }, 100);
            }
        },
        jPlayerReady: function (swfVersion) { // Called from Flash / HTML5 interval
            this.config.swfVersion = swfVersion;
            if (this.config.swfVersionRequired != this.config.swfVersion) {
                this._error("jPlayer's JavaScript / SWF version mismatch!\n\nJavaScript requires SWF : " + this.config.swfVersionRequired + "\nThe Jplayer.swf used is : " + this.config.swfVersion);
            }
            this._jPlayerReadyBackend("flash");
        },
        _jPlayerReadyBackend: function (backend) {
            if (this.config.usingBackends[backend] == 1) {
                this.config.usingBackends[backend] = 2;
                if (objAll(this.config.usingBackends, function (status, aBackend) { return status > 1; })) {
                    this.jPlayerReadyCustom();
                }
            }
        },

        jPlayerReadyCustom: function () {
            // Replaced by ready function from options in _init()
        },
        setFile: function (mp3, ogg) {
            this.loadSong([{ type: "audio/ogg", src: ogg }, { type: "audio/mpeg", src: mp3}]);
        },
        loadSong: function (song) {
            var self = this;
            $.each(song, function (i, songOption) {
                var backendsForType = self.config.support[songOption.type];
                if (backendsForType.length == 0) return true; //continue looking...
                var newBackend = backendsForType[0];
                if (self.handlers.current != newBackend) {
                    if (self.config.isFileSet)
                        self.handlers[self.handlers.current].clearFile(); //will stop.
                    self.handlers.current = newBackend;
                }
                self.handlers[self.handlers.current].loadSong(songOption.type, songOption.src, songOption.replaygain);
                return false; //no need to continue;
            });
        },
        clearFile: passToHandler(function (h) { h.clearFile(); }),
        play: passToHandler(function (h) { h.play(); }),
        pause: passToHandler(function (h) { h.pause(); }),
        stop: passToHandler(function (h) { h.stop(); }),
        playHead: passToHandler(function (h, p) { h.playHead(p); }),
        playHeadTime: passToHandler(function (h, t) { h.playHeadTime(t); }),
        volume: passToHandler(function (h, v) { h.volume(limitValue(v, 0, 100)); }),
        cssId: function (fn, id) {
            if (typeof id != 'string')
                this._warning("cssId CSS Id must be a string\n\njPlayer('cssId', '" + fn + "', " + id + ")");
            else if (!_cssIdDefaultsForActions[fn])
                this._warning("Unknown/Illegal function in cssId\n\njPlayer('cssId', '" + fn + "', '" + id + "')");
            else {
                if (this.config.cssId[fn] != undefined)
                    this.config.cssSelector[fn].unbind("click", this.config.clickHandler[fn]);
                var self = this;
                this.config.clickHandler[fn] = function (e) { self[fn](e); return false; };
                this.config.cssId[fn] = id;
                this.config.cssSelector[fn] = $("#" + id).click(this.config.clickHandler[fn]);
                this.config.cssDisplay[fn] = this.config.cssSelector[fn].css("display");
                if (fn == "pause") {
                    this.config.cssSelector[fn].css("display", "none");
                }
            }
        },
        loadBar: function (e) { // Handles clicks on the loadBar
            if (this.config.cssId.loadBar != undefined) {
                var offset = this.config.cssSelector.loadBar.offset();
                var x = e.pageX - offset.left;
                var w = this.config.cssSelector.loadBar.width();
                var p = 100 * x / w;
                this.playHead(p);
            }
        },
        playBar: function (e) { // Handles clicks on the playBar
            this.loadBar(e);
        },
        onProgressChange: function (fn) {
            if ($.isFunction(fn))
                this.onProgressChangeCustom = fn;
            else
                this._warning("onProgressChange parameter is not a function.");
        },
        onProgressChangeCustom: function () { }, // Replaced in onProgressChange()
        jPlayerOnProgressChange: function (lp, ppr, ppa, pt, tt) { // Called from Flash / HTML5 interval
            var diag = this.config.diag;
            diag.loadPercent = lp;
            diag.playedPercentRelative = ppr;
            diag.playedPercentAbsolute = ppa;
            diag.playedTime = pt;
            diag.totalTime = tt;
            this.config.cssSelector.loadBar.width(lp + "%"); //if loadBar doesn't exist, this doesn't do anything.
            this.config.cssSelector.playBar.width(ppr + "%");
            this.onProgressChangeCustom(lp, ppr, ppa, pt, tt);
            this._forceUpdate();
        },
        jPlayerController: function (override) { // The HTML5 interval function.
            var pt = 0, tt = 0, ppa = 0, lp = 0, ppr = 0;
            var audio = this.config.audio;
            if (this.config.audio.readyState >= 1) {
                pt = audio.currentTime * 1000; // milliSeconds
                tt = audio.duration * 1000; // milliSeconds
                tt = isNaN(tt) ? 0 : tt; // Clean up duration in Firefox 3.5+
                ppa = (tt > 0) ? 100 * pt / tt : 0;
                if (typeof audio.buffered == "object" && audio.buffered.length > 0) {
                    lp = 100 * audio.buffered.end(audio.buffered.length - 1) / audio.duration;
                    ppr = 100 * audio.currentTime / audio.buffered.end(audio.buffered.length - 1);
                } else {
                    lp = 100;
                    ppr = ppa;
                }
            }

            if (audio.ended) {
                clearInterval(this.config.jPlayerControllerId);
                this.jPlayerOnSoundComplete();
            } else if (!this.config.diag.isPlaying && lp >= 100) {
                clearInterval(this.config.jPlayerControllerId);
            }

            if (override) ppr = ppa = pt = 0;
            this.jPlayerOnProgressChange(lp, ppr, ppa, pt, tt);
        },
        volumeMin: function () {
            this.volume(0);
        },
        volumeMax: function () {
            this.volume(100);
        },
        volumeBar: function (e) { // Handles clicks on the volumeBar
            if (this.config.cssId.volumeBar != undefined) {
                var barEl = this.config.cssSelector.volumeBar;
                this.volume(100 * (e.pageX - barEl.offset().left) / barEl.width());
            }
        },
        volumeBarValue: function (e) { // Handles clicks on the volumeBarValue
            this.volumeBar(e);
        },
        jPlayerVolume: function (v) { // Called from Flash 
            this.jPlayerVolumeRaw(v * v / 100.0);
        },
        jPlayerVolumeRaw: function (v) { // Called from Flash / HTML5 event
            if (this.config.cssId.volumeBarValue != null) {
                this.config.cssSelector.volumeBarValue.width(Math.min(100.0, v / this.config.diag.gainScale) + "%");
                this._forceUpdate();
            }
        },
        onSoundComplete: function (fn) {
            if ($.isFunction(fn))
                this.onSoundCompleteCustom = fn;
            else
                this._warning("onSoundComplete parameter is not a function.");
        },
        onSoundCompleteCustom: function () { }, // Replaced in onSoundComplete()
        jPlayerOnSoundComplete: function () { // Called from Flash / HTML5 interval
            this.handlers.setButtons(false);
            this.onSoundCompleteCustom();
        },
        getData: function (name) {
            var val = this.config;
            $.each(name.split("."), function (i, part) {
                if (val != undefined)
                    val = val[part];
            });
            if (val == undefined)
                this._warning("Undefined data requested.\n\njPlayer('getData', '" + name + "')");
            return val;
        },
        _getMovie: function () {
            return document[this.config.fid];
        },
        _checkForFlash: function (version) {
            // Function checkForFlash adapted from FlashReplace by Robert Nyman
            // http://code.google.com/p/flashreplace/
            var flashIsInstalled = false;
            var flash;
            if (window.ActiveXObject) {
                try {
                    flash = new ActiveXObject(("ShockwaveFlash.ShockwaveFlash." + version));
                    flashIsInstalled = true;
                }
                catch (e) {
                    // Throws an error if the version isn't available			
                }
            }
            else if (navigator.plugins && navigator.mimeTypes.length > 0) {
                flash = navigator.plugins["Shockwave Flash"];
                if (flash) {
                    var flashVersion = navigator.plugins["Shockwave Flash"].description.replace(/.*\s(\d+\.\d+).*/, "$1");
                    if (flashVersion >= version) {
                        flashIsInstalled = true;
                    }
                }
            }
            return flashIsInstalled;
        },
        _forceUpdate: function () { // For Safari and Chrome
            if (this.config.graphicsFix)
                this.config.hSel.text("" + Math.random());
        },
        _flashError: function (e) {
            this._error("Problem with Flash component.\n\nCheck the swfPath points at the Jplayer.swf path.\n\nswfPath = " + this.config.swfPath + "\nurl: " + this.config.swf + "\n\nError: " + e.message);
        },
        _error: function (msg) {
            if (this.config.errorAlerts)
                this._alert("Error!\n\n" + msg);
        },
        _warning: function (msg) {
            if (this.config.warningAlerts)
                this._alert("Warning!\n\n" + msg);
        },
        _alert: function (msg) {
            alert("jPlayer " + this.config.version + " : id='" + this.config.id + "' : " + msg);
        }
    };
})(jQuery);
