<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:param name="vakcodes"/>
  <xsl:template match="/">
    <html>
      <head>
        <style type="text/css">

        </style>
      </head>
      <body>
        <xsl:call-template name="makeMyDay">
          <xsl:with-param name="day">maandag</xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="makeMyDay">
          <xsl:with-param name="day">dinsdag</xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="makeMyDay">
          <xsl:with-param name="day">woensdag</xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="makeMyDay">
          <xsl:with-param name="day">donderdag</xsl:with-param>
        </xsl:call-template>
        <xsl:call-template name="makeMyDay">
          <xsl:with-param name="day">vrijdag</xsl:with-param>
        </xsl:call-template>
      </body>
    </html>
  </xsl:template>

  <xsl:template name="makeMyDay">
    <xsl:param name="day"/>
    <table class="{$day} day">
      <thead>
        <tr>
          <th colspan="4">
            <xsl:value-of select="$day"/>
          </th>
        </tr>
        <tr>
          <th>when</th>
          <th>weeks</th>
          <th>where</th>
          <th>what</th>
        </tr>
      </thead>
      <tbody>
        <xsl:for-each select="/entries/entry[@dag=$day][contains($vakcodes,@vakcode)]">
          <xsl:sort select="@time"/>
          <xsl:variable name="time" select="@time"/>
          <xsl:variable name="loc" select="@loc"/>
          <xsl:variable name="wat" select="@wat"/>
          <xsl:variable name="vakcode" select="@vakcode"/>
          <xsl:variable name="weken" select="@weken"/>
          <xsl:if test="not(preceding-sibling::*[@time=$time and @loc=$loc and @wat=$wat and @vakcode=$vakcode and @weken=$weken])">

            <tr>
              <td>
                <xsl:value-of select="@time"/>
              </td>
              <td>
                <xsl:value-of select="@weken"/>
              </td>
              <td>
                <xsl:value-of select="@loc"/>
              </td>
              <td>
                <xsl:value-of select="@wat"/>
                <xsl:text> </xsl:text>
                <a href="{@vaklink}">
                  <xsl:value-of select="@vaknaam"/>
                </a>
              </td>
            </tr>
          </xsl:if>
        </xsl:for-each>
      </tbody>
    </table>
      
  </xsl:template>

</xsl:stylesheet> 

