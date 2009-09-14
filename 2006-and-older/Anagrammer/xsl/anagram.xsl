<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="matches">
    <html>
      <head>
        <title>Anagrams!</title>
        <style type="text/css">
          @import 'css/main.css';
        </style>
      </head>
      <body onload="document.getElementById('startHere').focus();">
        <div class="outercontainer">
          <div class="innercontainer">
            <div class="content">
              <form method="get">
                <p >
                  This web page finds anagrams! The dictionary contains <xsl:value-of select="@dictsize" /> words. There may be <a href=".">more dictionaries</a>.
                </p>
                <p>
                  You can search manually:<br/>
                  Word:<input name="word" type="text" size="50" id="startHere"/><br/>
                  <input type="submit" value="Look up"/>
                </p>
                <p>
                  Or, you can try my suggestion: <a href="?word={@try}">
                    <em>
                      <xsl:value-of select="@try"/>
                    </em>
                  </a>
                </p>
              </form>
            </div>
            <div class="content" >
            Your anagrams! (Click on a word to look up it's definition in google...)
            <ul>
              <xsl:apply-templates select="match" />
            </ul>
            </div>
          </div>
        </div>
      </body>
    </html>
	</xsl:template>
	<xsl:template match="dictsize"></xsl:template>
	<xsl:template match="match">
		<li>
      <a href="http://www.google.com/search?q=define:{.}&amp;safe=off&amp;oi=definel&amp;defl=all">
        <xsl:value-of select="." />
      </a>
		</li>
	</xsl:template>
</xsl:stylesheet>
