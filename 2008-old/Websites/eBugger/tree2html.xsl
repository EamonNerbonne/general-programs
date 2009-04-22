<?xml version="1.0" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method ="html" doctype-public="-//W3C//DTD XHTML 1.0 Strict//EN" indent="no"/>
  <xsl:strip-space elements="*"/>
  <xsl:template match="/">
    <html>
      <head>
        <title>This is a static Tree test</title>
        <link rel="stylesheet" type="text/css" href="tree.css"/>
        <script type="text/javascript" src="simple.js"> </script>
      </head>
      <body onload="initTree()">
          <xsl:apply-templates select="*" />
      </body>
    </html>
  </xsl:template>
  
  <xsl:template match="node">
    <div class="tree collapsed">
      <table>
        <!--<col/><xsl:for-each select="col"><col align="char" char="." color="red"/></xsl:for-each>-->
        <thead>
          <tr>
            <th class="symcol"><img src="imin.png" class="whenexpanded"/><img src="iplus.png" class="whencollapsed"/></th>
            <th><xsl:value-of select="col[1]" /></th>
            <xsl:for-each select="col[position()&gt;1]">
              <td>
                <xsl:value-of select="." />
              </td>
            </xsl:for-each>
          </tr>
        </thead>
        <tbody>
          <!--<xsl:variable name="sortcol1" select="count(col[@sortprior='1']/preceding-sibling::col)+1"/>-->
          <!--<xsl:variable name="sorttype1" select="col["/>-->
          <xsl:for-each select="leaf">
           <!--<xsl:sort select="@*[$sortcol1]"/>-->
            <tr>
              <xsl:if test="position() mod 2 = 1"><xsl:attribute name="class">odd</xsl:attribute></xsl:if>
              <th class="symcol"/><th><a href="http://www.e-bug.de/cgi-ssl/preise.cgi?fts={@col2}" target="_new"><xsl:value-of select="@*[1]" /></a></th>
              <xsl:for-each select="@*[position()&gt;1]">
                <td>
                  <xsl:value-of select="." />
                </td>
              </xsl:for-each>
            </tr>
          </xsl:for-each>
        </tbody>
      </table>
      <xsl:apply-templates select="node" />
    </div>
  </xsl:template>
</xsl:stylesheet>