<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:output method="xml"/>
  <xsl:template match="/">
    <xsl:apply-templates select="*"/>
  </xsl:template>
  <xsl:template match="@*|text()">
    <xsl:copy/>
  </xsl:template>
  <xsl:template match="*">
    <xsl:copy>
      <xsl:apply-templates select="*|@*|text()"/>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="a[@class='spell']">
    <emph>
      <xsl:apply-templates select="*|text()"/>
    </emph>
  </xsl:template>
  <xsl:template match="a">
      <xsl:apply-templates select="*|text()"/>
  </xsl:template>
  <xsl:template match="h4">
    <strong>
      <xsl:apply-templates select="*|@*|text()"/>
    </strong>
  </xsl:template>
  <xsl:template match="h6">
    <h2>
      <xsl:apply-templates select="*|@*|text()"/>
    </h2>
  </xsl:template>

</xsl:stylesheet>
