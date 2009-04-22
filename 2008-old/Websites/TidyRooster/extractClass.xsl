<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
  <entries>
    <xsl:apply-templates select="//tr[td/b/a[contains(@href,'/vakken/')]]"/>
  </entries>
</xsl:template>

  <xsl:template match="tr">
    <entry time="{td[1]}" loc="{td[2]}" wat="{td[3]}" vaklink="{td[4]/b/a/@href}" vakcode="{substring-after(td[4]/b/a/@href,'/vakken/')}" 
      vaknaam="{normalize-space(td[4])}" weken="{following-sibling::tr/td[3]}" dag="{(preceding-sibling::tr[th/@colspan='9']/th)[position()=last()]}"/>
  </xsl:template>
                
</xsl:stylesheet> 

