// Pop-Up Embedder Script by David Battino, www.batmosphere.com
// Version 2008-08-21     
// Inspired by Delicious Play Tagger and Yahoo Media Player
// OK to use if this notice is included

// To do: move embedding function to external script to fix IE's "Click to Activate" error. Detect image height for Opera.

window.onload = function(){ // Add pop-up trigger to audio links 
var links=document.getElementsByTagName('a');
for (var i=0,o;o=links[i];i++){
if(o.href.match(/\.mp3$/i)||o.href.match(/\.wav$/i)||o.href.match(/\.aif$/i)||o.href.match(/\.aiff$/i)||o.href.match(/\.m3u$/i)||o.href.match(/\.wma$/i)||o.href.match(/\.mid$/i)||o.href.match(/\.ogg$/i)){
// insert bullet character in front of playable audio links (optional)
var bullet = document.createElement('b');
	bullet.innerHTML = '&raquo;'; // &raquo; = >> | &#9834; = 8th note
	bullet.style.color = '#00ee00';
	bullet.style.padding = '0px 1px';
    bullet.style.backgroundColor = 'black';
	bullet.style.border = '1px solid #00ee00';
	bullet.style.marginRight = '4px';
	o.parentNode.insertBefore(bullet,o);
// end of bullet section
	o.setAttribute("onclick","javascript:BatPop(this.id,this.title,this.href,this.firstChild);return false;"); // window name, caption, sound URL, image object (in link)
}}}
   
function BatPop(windowName,caption,soundpath,imgObj) { 
//alert('name='+windowName+'\n\ncaption='+caption+'\n\nsoundpath=' +soundpath+'\n\nheight=' +imgObj.height+ '\n\nimg path='+imgObj.src);
	 var winWidth = 350;
 	 var imgborder = 0;
	 var imgwidth = 250;
	 var imgheight = 0; // assume there's no image
	 if (imgObj) {
	 	if (imgObj.height) {
	 		imgScaleHeight = (imgwidth * imgObj.height / imgObj.width); // calculate scaled height by ratio
	 		imgheight = Math.round(imgScaleHeight * Math.pow(10, 0)) / Math.pow(10, 0); // round to integer for Safari
	 		imgborder = 2;
	 	};
	 	if (imgObj.src) { var imgpath = imgObj.src };
	 }
     var rawHeight = imgheight + 180 + caption.length/3; // calculate window height based on caption length and image height
     var winHeight = Math.round(rawHeight * Math.pow(10,0))/Math.pow(10,0); 
 
// Get Operating System 
var isWin = navigator.userAgent.toLowerCase().indexOf("windows") != -1
if (isWin) { // Use MIME type = "application/x-ms-wmp";
    visitorOS="Windows";
    winHeight = winHeight + 50;
    controllerHt=65; // Windows Media plug-in height
} else { // Use MIME type = "audio/mpeg"; // or audio/x-wav or audio/x-ms-wma, etc.
    visitorOS="Other";
    controllerHt=16; // QuickTime plug-in height
}

// Get the MIME type of the audio file from its extension (for non-Windows browsers)
var mimeType = "audio/mpeg"; // assume MP3/M3U
var objTypeTag = "application/x-ms-wmp"; // The Windows MIME type to load the WMP plug-in in Firefox, etc.
var theExtension = soundpath.substr(soundpath.lastIndexOf('.')+1, 3); // truncates .aiff to aif
if (theExtension.toLowerCase() == "wav") { mimeType = "audio/x-wav"};
if (theExtension.toLowerCase() == "aif") { mimeType = "audio/x-aiff"}; 
if (theExtension.toLowerCase() == "wma") { mimeType = "audio/x-ms-wma"};
if (theExtension.toLowerCase() == "mid") { mimeType = "audio/x-midi"};
if (theExtension.toLowerCase() == "ogg") { mimeType = "application/ogg"}; // try others
if (theExtension.toLowerCase() == "m3u") { mimeType = "audio/x-mpegurl"}; // may not be necessary
// Add additional MIME types as desired

if (!windowName){windowName = theExtension.toUpperCase() + " Player"};

var autostart = document.getElementById('autostart').checked;

if (visitorOS != "Windows") { 
objTypeTag = mimeType; // audio/mpeg, audio/x-wav, audio/x-ms-wma, etc.
};
PlayerWin = window.open('', 'idxStatus'); //,'width='+winWidth+',height='+winHeight+',top=0,left=0,screenX=0,screenY=0'
    PlayerWin.resizeTo(winWidth,winHeight); // resize window to caption if window already exists
    PlayerWin.document.write("<html><head><title>" + windowName + "</title></head>");
    PlayerWin.document.write("<body bgcolor='#9E9E9E'; text='#000000'; >"); // specify background image if desired
    PlayerWin.document.write("<div align='center'>");
   // PlayerWin.document.write("<h3>"+windowName+"</h3>");
    //PlayerWin.document.write("<img src='" + imgpath + "' border='"+imgborder+"' alt='' width='" + imgwidth + "' /><br />");
    PlayerWin.document.write("<object classid='clsid:6BF52A52-394A-11D3-B153-00C04F79FAA6'>"); // ClassID forces IE to use Windows Media Player
    PlayerWin.document.write("<param name='url' value=\"" +  soundpath + "\">"); // data, src and filename are alternatives to url
    PlayerWin.document.write("<param name='type' value='" + objTypeTag + "'>");
    PlayerWin.document.write("<param name='autoStart' value='0'>");
    PlayerWin.document.write("<param name='showcontrols' value='1'>");
    PlayerWin.document.write("<param name='showstatusbar' value='1'>");
    PlayerWin.document.write("<embed src =\"" + soundpath + "\" type='" + objTypeTag + "' autoplay='false' autostart='"+autostart+ "' style='width:100%;height:100%;'  controller='1' showstatusbar='1' bgcolor='#000000' kioskmode='true'>");
    PlayerWin.document.write("</embed></object>");

    PlayerWin.document.write("<div style='width: " + imgwidth + "px; text-align:left;'>"); // restrict caption width to image width
    PlayerWin.document.write("<p style='font-size:12px;font-family:Verdana,sans-serif'>" + caption + "</p>");
    PlayerWin.document.write("</div>");
    PlayerWin.document.write("<p style='font-size:12px;font-family:Verdana,sans-serif'><a href=\"" + soundpath + "\">Download audio</a> <span style='font-size:10px'>(right-click)</span>");
    PlayerWin.document.write(" &#8226; <a href='#' onClick='javascript:window.close();'>Close window</a></p>");
    PlayerWin.document.write("</div>");
    PlayerWin.document.write("</body></html>");
    PlayerWin.focus();
    PlayerWin.document.close(); // "Finalizes" new window
}