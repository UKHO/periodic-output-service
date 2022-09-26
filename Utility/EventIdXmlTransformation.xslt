<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">

    <xsl:output method="html" indent="yes"/>

    <xsl:template match="/">

        <table border="1" width="85%" cellspacing="0" cellpadding="2" frame="box">
            <thead>
                <th>Event id</th>
                <th>Description</th>
            </thead>

            <xsl:for-each select="/doc/members/member[contains(@name,'UKHO.PeriodicOutputService.Common.Logging.EventIds.')]">
                <tr>
                    <td>
                        <xsl:value-of select="substring-before(summary,' - ')"/>
                    </td>
                    <td>
                        <xsl:value-of select="substring-after(summary,' - ')"/>
                    </td>
                </tr>
            </xsl:for-each>
        </table>

    </xsl:template>
</xsl:stylesheet>
