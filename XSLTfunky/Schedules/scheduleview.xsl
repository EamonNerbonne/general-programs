<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:key name="subjname" match="//ref/act/@name" use="parent::*/@id"/>
 <xsl:key name="subjclass" match="//ref/act/@class" use="parent::*/@id"/>
 <xsl:template match="/schedule">
  <html>
   <head>
    <title>Schedule</title>
    <link rel="stylesheet" type="text/css" href="scheduleview.css"/>
   </head>
   <body><xsl:apply-templates select="week"/></body>
  </html>
 </xsl:template>
 <xsl:template match="week">
  <div><xsl:copy-of select="@class"/><xsl:if test="@name"><h3><xsl:value-of select="@name"/></h3></xsl:if>
  <table>
   <thead>
   <tr><th>Time</th><th>Place</th><th>Activity</th></tr>
   </thead>
   <tbody><xsl:apply-templates select="day"/></tbody>
  </table>
  </div>
 </xsl:template>
 <xsl:template match="day">
  <tr><td colspan="3" class="day"><xsl:value-of select="@name"/></td></tr>
  <xsl:apply-templates select="frame">
   <xsl:sort select="@start" data-type="text"/>
  </xsl:apply-templates>
 </xsl:template>
 <xsl:template match="frame">
  <tr>
   <xsl:copy-of select="key('subjclass',@what)"/>
   <td><xsl:value-of select="concat(@start,' - ',@end)"/></td>
   <td><xsl:value-of select="@loc"/></td>
   <td><xsl:value-of select="concat(key('subjname',@what),' (',@type,')')"/>
   <xsl:if test="@note"> - <xsl:value-of select="@note"/></xsl:if></td>
  </tr>
 </xsl:template>
</xsl:stylesheet>