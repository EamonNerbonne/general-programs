<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:template match="test">
  <p>
   Test attribute with &quot;desc&quot; attribute found!
   Desc contains: <xsl:value-of select="@desc"/>
  </p>
 </xsl:template>
</xsl:stylesheet>