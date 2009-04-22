<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:param name="periode"/>

<xsl:template match="/">
  <root>
    <xsl:for-each select="//table/tbody/tr/td/a[text()=$periode]/@href">
      <href>
        <xsl:value-of select="."/>
      </href>
    </xsl:for-each>
  </root>
</xsl:template>

</xsl:stylesheet> 
