<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="/">
		<html>
			<body>
				<table style="width:90%; border-collapse:collapsed;">
					<colgroup>
						<col style="width:30%"/>
						<col style="background:#ddd;width:40%"/>
						<col style="width:5%"/>
						<col style="width:25%"/>
					</colgroup>
					<tr>
						<th>Artist</th>
						<th>Title</th>
						<th>Track</th>
						<th>Album</th>
					</tr>
					<xsl:apply-templates select="*"/>
				</table>
			</body>
		</html>
	</xsl:template>

	<xsl:template match="songs">
		<xsl:apply-templates select="*"/>
	</xsl:template>

	<xsl:template match="partsong|songref|song">
		<tr onclick="top.frames['idxStatus'].location.href='{@songuri}'" style="cursor:pointer;">
			<xsl:choose>
				<xsl:when test="@artist">
					<td>
						<xsl:value-of select="@artist"/>
					</td>
					<td>
						<xsl:value-of select="@title"/>
					</td>
					<td>
						<xsl:value-of select="@track"/>
					</td>
					<td>
						<xsl:value-of select="@album"/>
					</td>
				</xsl:when>
				<xsl:otherwise>
					<td colspan="4" >
						<xsl:choose>
							<xsl:when test="string-length(@label) &gt; 0">
								<xsl:value-of select="@label"/>
							</xsl:when>
							<xsl:otherwise>
								<xsl:call-template name="replace">
									<xsl:with-param name="str" select="@songuri"/>
									<xsl:with-param name="what" >
										<xsl:text>%20</xsl:text>
									</xsl:with-param>
									<xsl:with-param name="with" >
										<xsl:text> </xsl:text>
									</xsl:with-param>
								</xsl:call-template>
							</xsl:otherwise>
						</xsl:choose>
					</td>
				</xsl:otherwise>
			</xsl:choose>
		</tr>
	</xsl:template>


	<xsl:template name="replace">
		<xsl:param name="str" />
		<xsl:param name ="what"/>
		<xsl:param name="with"/>
		<xsl:choose>
			<xsl:when test="contains($str,$what)">
				<xsl:variable name="restRep">
					<xsl:call-template name="replace">
						<xsl:with-param name="str" select="substring-after($str,$what)"/>
						<xsl:with-param name="what" select="$what"/>
						<xsl:with-param name="with" select="$with"/>
					</xsl:call-template>
				</xsl:variable>
				<xsl:value-of select="concat(substring-before($str,$what),$with,$restRep)"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$str"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>