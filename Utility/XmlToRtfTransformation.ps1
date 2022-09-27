param (
    [Parameter(Mandatory = $true)] [string] $xmlFilePath, 
    [Parameter(Mandatory = $true)] [string] $xsltFilePath, 
    [Parameter(Mandatory = $true)] [string] $outputFileName)

    #Validate params
    if (-not $xmlFilePath -or -not $xsltFilePath -or -not $outputFileName)
    {
        Write-Host "Invalid parameters provided."
        throw
    }

    Try
    {
        #Set outputfile path        
         $outputHTMLFile = $xsltFilePath.Substring(0, $xsltFilePath.LastIndexOf('\'))+"\"+$outputFileName+".html"

        $xslt_settings = New-Object System.Xml.Xsl.XsltSettings;
        $XmlUrlResolver = New-Object System.Xml.XmlUrlResolver;

        $xslt_settings.EnableScript = 1;

        $xslt = New-Object System.Xml.Xsl.XslCompiledTransform;

        Write-Host "Generating html file."
        #XML Transforms
        $xslt.Load($xsltFilePath,$xslt_settings,$XmlUrlResolver);
        $xslt.Transform($xmlFilePath, $outputHTMLFile);

        if (Test-Path $outputHTMLFile)
        {
            Write-Host "HTML file is generated."
        }

        #Set RTF file path
        $rtfFile = $outputHTMLFile.Substring(0,$outputHTMLFile.LastIndexOf(".html"))+".rtf"

        #Delete RTF file, if exists
        if (Test-Path $rtfFile) 
        {
            Remove-Item $rtfFile
        }

         #Rename html to rtf 
         Rename-Item $outputHTMLFile $rtfFile  
         
        if (Test-Path $rtfFile) 
        {
            Write-Host "Rtf file is created."
        }                
    }
    Catch
    {
        $ErrorMessage = $_.Exception.Message
        $FailedItem = $_.Exception.ItemName
        Write-Host  'Error'$ErrorMessage':'$FailedItem':' $_.Exception; 
        throw
    }
