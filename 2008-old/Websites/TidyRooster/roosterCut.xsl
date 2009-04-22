<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="/">
    <roosterCut>
      <xsl:for-each select="//h2[text()='Rooster']/following-sibling::*[1]/self::div[@class='tableContainer']/table">
        <xsl:copy-of select="."/>
      </xsl:for-each>
    </roosterCut>
  </xsl:template>

</xsl:stylesheet>

