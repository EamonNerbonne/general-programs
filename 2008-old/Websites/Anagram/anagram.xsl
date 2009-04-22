<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:template match="matches">
  <html>
   <head>Anagrams!</head>
   <body onload="document.getElementById('startHere').focus();">
    <form method="get">
     <ol>
      <li> Word:<input name="word" type="text" size="50" id="startHere"/></li>
      <li>     <input type="submit" value="Look up"/></li>
     </ol>
    </form>
    Dictionary size: <xsl:value-of select="@dictsize" /> words.<br />
    <ul>
     <xsl:apply-templates select="match" />
    </ul>
   </body>
  </html>
 </xsl:template>
 <xsl:template match="dictsize"></xsl:template>
 <xsl:template match="match">
  <li>
   <xsl:value-of select="." />
  </li>
 </xsl:template>
</xsl:stylesheet>
