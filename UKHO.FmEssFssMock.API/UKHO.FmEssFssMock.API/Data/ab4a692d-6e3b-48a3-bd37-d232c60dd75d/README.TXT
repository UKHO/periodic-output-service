AVCS README
File date: 2024-04-04 09:59:14Z
This file Version 20230531-01 was last updated 31-May-2023

Latest significant changes:
31-May-2023  COMPLIANCE STATEMENT ADDED TO SECTION 1; ADDED D2 INFORMATION
26-Jan-2023  NUMBER OF PERMITTED INSTALLATIONS UPDATED
08-Dec-2022  ADDED RECENT NEW PRODUCERS TO PERMITTED INSTALLATIONS LIST
20-Oct-2022  ALBANIAN ENCs ADVICE FOR JRC USERS IS RESOLVED
18-Aug-2022  EXTRA CONTOURS IN HIGH DENSITY ENCs
21-Jul-2022  DISPLAY ISSUE WITH ALBANIAN AL ENCs ON JRC ECDIS
05-May-2022  COMBINED AVCS AND AIO README FILES AND GENERAL UPDATES


---------------------

USING THE README.TXT FILE

This README.TXT file contains the latest information available on the use of ENCs in ADMIRALTY services.  This information may be navigationally significant and should be reviewed for changes when you update your ECDIS.

Further potentially significant, but less frequently updated, information is contained in the file Important-Information-for-AVCS-users.pdf located on the Support tab at admiralty.co.uk/AVCS.  This file includes general cautions on the use of ENCs and ECDIS.  This file will be updated when necessary and changes will be announced in this README.TXT file. The latest version is 22-Jul-2020.

The date/time in the "File date" in the header of this README.TXT is inserted when the data exchange set is created. The most recent change to the file is stated in line 3. For technical reasons, the date of the README.TXT file provided in Weekly Update exchange sets may be some days before the date that the Exchange Set is valid. The README.TXT file that is available to download from www.admiralty.co.uk is that which is included in the current Weekly Update exchange sets. 
Later versions of the README.TXT file and later date/time stamps may be included in Exchange sets created in the ADMIRALTY Digital Delivery Service (See below). 

---------------------

CONTENTS:
SECTION 1 - IMPORTANT GENERAL INFORMATION - Updated 31-May-2023
SECTION 2 - HYDROGRAPHIC OFFICES´ INFORMATION - Updated 31-May-2023
SECTION 3 - WITHDRAWN CELLS - Updated 07-May-2020
SECTION 4 - MISCELLANEOUS INFORMATION - Updated 07-May-2020
=============

SECTION 1 - IMPORTANT GENERAL INFORMATION
--------------------------------------- 

COMPLIANCE
-----------
ENC Data only meets SOLAS chart carriage requirements if it is kept up to date and is used on an ECDIS with a suitable back up arrangement.


DISPLAY ANOMALIES 
-----------------
Users of Transas ECDIS version 3.0.340 Build 5225 should be aware that a small number of ENCs with PG and SB producer codes do not display as expected. For more details, please see SECTION 2 below.


ISOLATED DANGER MAY NOT DISPLAY AS EXPECTED
-------------------------------------------
UKHO has been made aware of a safety issue where the Isolated Danger symbol is not displaying as expected on some ECDIS in certain rare circumstances. 

This issue results from the way some ECDIS are processing some ENC updates. It will be resolved by issuing a New Edition of the affected ENCs and software upgrades to affected ECDIS. ENC Producers and Regional ENC Coordination Centres have been made aware of the issue and the affected ENCs will be updated by New Edition as soon as possible. 

The display issue does not affect the detection of isolated dangers in route checks or monitoring/anti-grounding checks. The ECDIS Pick Report is able to access the information about the hazard as normal.
The correct symbol will display if the ECDIS is in display mode ‘ALL OTHER’.

Until the measures above have taken effect, mariners should continue to respond to alerts and indications as normal but be aware that, in rare situations, an Isolated Danger symbol may not be showing on the display as would have been expected. If the yellow hazard highlight box is observed without a symbol, a manual update can be applied to insert the feature.

EXTRA CONTOURS IN HIGH DENSITY ENCs
-----------------------------------
Unlike most standard ENCs, HD (High Density) ENCs contain extra contours at intervals of 1 metre or less. This allows the safety contour to more closely match the ECDIS setting applied by the user and therefore give a more accurate representation of safe water. The ENC Producer may use SCAMIN (minimum viewing scale attribute) to hide the extra contours and to reduce clutter from the ECDIS display.  Users are advised to set their ECDIS Safety Contour to align with their planned under keel clearance, instead of manually rounding to the nearest 5 or 10 metres. The ECDIS will then automatically depict the appropriate safe water if HD contours are available, according to the needs of the passage.

HD contours are drawn from high quality surveys with the appropriate CATZOC applied. Whilst CATZOC provides useful information, it should not be relied on in isolation, particularly where the seabed is very mobile, such as areas of sand waves or in river entrances. The user should use the ENC pick report to interrogate any further information that may have been provide by the ENC producer within M_QUAL, M_SREL and M_ACCY objects to establish the date and accuracy of the survey data. This should be used to build a picture of the appropriate safety tolerances when determining the best passage plan.



NUMBER OF PERMITTED INSTALLATIONS
----------------

The End User Licence Agreement (EULA) governing the use of AVCS data permits its installation on an unlimited number of workstations on any licensed vessel.  It further recognises that some parts of the data may be installed on fewer workstations.  ENCs with the following producer codes may be installed on the maximum number of workstations indicated:

Up to 3 workstations
C2, MS, SG, TH

Up to 5 workstations
C1, C3, CN, EA, ID, IN, JP, KH, KR, UA, GBV, LK, MM, 

Up to 10 workstations
CA

Unlimited workstations on a single vessel or 5 workstations at a single shore-based location
AL, AR, AU, BD, BE, BH, BR, CK, CL, CO, CU, D2, DE, DK, EC, EE, EG, ES, FI, FR, GB, GE, GR, HR, I1, IR, IS, IT, LB, LT, LV, ME, MT, MX, MY, NG, NL, NO, NU, NZ, OM, PA, PE, PG, PH, PK, PL, PT, RO, RU, S1, SB, SE, SI, SR, TK, TN, TO, TR, US, UY, V1, VE, VN, WS, ZA
Also: additional datasets from 1U

Shorebased licences allow up to 5 workstations. 

---------------------

GEOGRAPHICAL NAMES - UK GOVERNMENT POLICY

Some of the chart data in this product have been compiled by other hydrographic offices. In accordance with principles adopted by the International Hydrographic Organization, the UKHO has not, without the permission of the data producer, made any amendments to such data. Consequently, some of the names used to depict geographical features do not reflect the policy of the United Kingdom in relation to the naming of such features. Where this is the case, it in no way implies that the United Kingdom accepts that the geographical names used are appropriate, or that it approves of their use.
 
---------------------

UPDATING AVCS AND AIO

Weekly AVCS and AIO update exchange sets can be downloaded from https://fss.admiralty.co.uk

---------------------

ADMIRALTY INFORMATION OVERLAY

The Admiralty Information Overlay (AIO) is an aid to navigators for use during passage planning and is available free of charge to all AVCS subscribers.  It contains ADMIRALTY Temporary & Preliminary Notices to Mariners (T&P NMs) for paper charts where the ENC producer does not include temporary information in their ENCs. It also highlights navigationally significant differences between ENCs and ADMIRALTY paper charts.

The T&P NMs shown in the Overlay relate specifically to ADMIRALTY paper charts and may not include all T&P NMs issued by the ENC producer.  There may also be a time delay between the publication or cancellation of T&P NMs by the ENC producer and the publication or removal of the equivalent ADMIRALTY T&P NMs.  Many ENC producers include some or all of their T&P NMs in their ENCs and care should be taken to understand what has already been included in the ENCs in use.  A list of ENC producers whose policy is to include T&P NM information in their ENCs can be found at www.admiralty.co.uk/AVCS.

Areas where the UKHO does not publish T&P NMs at the visible scale are highlighted with a ‘No Overlay’ feature.  Additional information, such as national T&P NMs, may be available in these areas from other sources and mariners should ensure that all appropriate sources of information have been consulted.

Please note that official ENCs remain the primary source of information for navigation at all times.  Whilst AIO is not a formal requirement of Port State Control, many mariners find that it is a useful way of demonstrating that they are taking note of and managing temporary information in their ENCs.

The Overlay can be displayed on compatible ECDIS equipment and also in some other software applications.  Please visit www.admiralty.co.uk/AVCS for an up to date list of compatible equipment, or consult your ECDIS supplier.


Information from UKHO for inspectors regarding T&P compliance when using AIO
---------------------------------------------------------------------------------

Managing Temporary & Preliminary Notices with ADMIRALTY INFORMATION OVERLAY (AIO) 

To comply with SOLAS Chapter V, the vessel should consider all available navigational information relevant to the passage. Appropriate scale, accurate and up-to-date charts are to be used for the intended passage, as well as any relevant temporary Notices to Mariners.

The AIO is an additional digital dataset that is provided to ADMIRALTY Vector Chart Service (AVCS) customers. It is designed to display temporary and preliminary information over ENCs in ECDIS and other chart display systems to assist with passage planning. Most countries now include temporary information in their ENCs and it is received on the vessel as a normal ENC update or new edition. Some nations do not describe the information as ‘temporary’ or ‘preliminary’ in the ENCs; they simply issue updates as required. The T&P policies of AVCS countries can be found on the Support tab at admiralty.co.uk/avcs and in the INFO folder of AIO discs.

The ECDIS Route Assessment tools will automatically assess the temporary information in ENCs because it appears as standard ENC objects and can be accessed from the ECDIS pick reports. Where the information is encoded in the ENC, digital chart users do not need to refer to ADMIRALTY T&P NMs that have been issued for paper charts. For the benefit of the navigator, AIO provides extra information in the form of ADMIRALTY T&Ps where the ENCs do not contain temporary information, and where there is ADMIRALTY paper chart coverage. To avoid duplication, AIO does not show ADMIRALTY paper T&Ps where the producer nation includes temporary information in their ENCs.

---------------------

ADMIRALTY Digital Delivery Service (ADDS)

ADDS is a new and faster way to obtain the ENC updates that do not require new permits. ENC Updates are released to our Distributors as soon as they are ready for distribution and not held back until the next weekly batch.
Vessels subscribed to ADDS will therefore not be tied to Weekly Updates and will receive some navigational information much earlier and more frequently than before. For more information, please contact your ADMIRALTY Distributor. For compliance purposes, your ECDIS should continue to have ENC updates that match, or are newer than, those listed in the latest PRODUCTS.TXT file supplied with your data.

---------------------


INTERIM CATEGORY OF ZONE OF CONFIDENCE IN DATA (CATZOC) VALUES

The boundaries between Quality of Data (M_QUAL) areas with different CATZOC values for some ENCs have been taken from the Source Diagram (SD) on the equivalent paper chart and they should therefore be considered to be approximate.

For these M_QUAL areas, the CATZOC values are based on the simplified summary provided in the SDD and therefore represent the lowest categorisation assigned to any survey in each defined group of surveys.

The simplification means that it has not been possible to identify minor gaps within survey coverage and mariners should be aware that such gaps (where lower quality survey data has been used in the compilation of the chart) may exist.

These assessments are based on original survey accuracies and on expectations of good practice by relevant survey authorities. Some evaluations may require downgrading once detailed survey information has been examined.

---------------------

OVERLAPPING ENCS IN THE SAME USAGE BAND

Some data providers issue ENCs which contain overlapping data coverage, both internally and with adjoining nation’s coverage. This may make them difficult to use in certain ECDIS systems which default to displaying both overlapping cells. Caution should therefore be exercised when using such overlapping data. Mariners should be aware that there will be a possibility that items of significance may be present only in one data set within an area of overlap. They should therefore take care to examine both data sets when planning a voyage through such an area.

=============

SECTION 2 - HYDROGRAPHIC OFFICES´ INFORMATION

---------------------

AU (Australia), PG (Papua New Guinea) and SB (Solomon Islands) ENC (published by AHO) - Updated 07 January 2022

Australian Hydrographic Office (AHO) publishes AU, PG and SB ENCs covering Australian, Australian Antarctic Territory, Papua New Guinea and Solomon Islands waters.

CONVERSION OF AU or SB-CODED ENCS TO PG-CODED ENCS – Updated 07 January 2022

A programme of work is underway to convert certain AU and SB-coded ENCs to PG-coded ENCs. As part of the testing of these new ENCs, it has been identified that there is the potential for a small number of ECDIS users to experience display problems with the adjusted AU / SB-coded ENCs on ECDIS Transas version 3.0.340 Build 5225. Users are advised to use caution and examine these ENCs at the voyage planning stage. 
The ENCs are: PG407156/SB407156, PG407155/SB407155, PG408155/SB408155, PG308154/SB308154, PG311145/AU311145, PG311144/AU311144, PG310144/AU310144, PG410143/AU410143, PG410142/AU410142, PG410141/AU410141, PG411141/AU411141, PG411140/AU411140, PG310140/AU310140.


GUIDANCE ON ECDIS FOR SHIPS CALLING AT AUSTRALIAN PORTS – 9 April 2020 

When entering Australian Ports please be reminded of the AMSA Marine Notice 7/2017 (https://www.amsa.gov.au/vessels-operators/regulations-and-standards-vessels/marine-notices) which highlights areas relevant to the safe and proper operation of ECDIS for ships visiting Australian ports and provides information on AMSA's expectations for ECDIS compliance during Port State Control (PSC) inspections.  

Ships found not complying with AMSA Marine Notice 7/2017 may be detained. 

AMSA are concerned with software quality assurance in ECDIS and detection of operating anomalies using the ECDIS Data Presentation and Performance Check dataset.  Other areas of interest include chart carriage requirements, mode of ECDIS operation and ECDIS training requirements.  Specifically PSC inspectors will take account of: 

* appropriate declaration of ECDIS equipment in the Record of Equipment - Safety Equipment Certificate 
* listing of ECDIS as critical equipment in the Safety Management System (SMS) 
* documented procedures and instructions for use of ECDIS are included in the SMS and that they are understood by all officers responsible for navigation 
* ECDIS type approval certificate, confirming compliance with relevant IMO performance standard and the International Electrotechnical Commission (IEC) test standard 
* ECDIS software is maintained to the latest applicable IHO standards 
* use of latest edition official ENCs, updated and corrected to the latest available updates and notices to mariners 
* adequate independent back-up arrangements (as detailed on the Record of Equipment), ensuring safe navigation for the remainder of the voyage in the event of an ECDIS failure 
* approved ECDIS generic and familiarisation training has been undertaken by the master and officers in charge of navigation 
* conformance and alignment with input from sensors (e.g. heading, speed and rate of turn) and presentation of such information on the ECDIS display 
* evidence of periodic tests and checks of the ECDIS carried out in accordance with the SMS and the manufacturer's requirements 
* demonstration of operational competency by the ship's navigating officers (e.g. safety checking of a voyage plan) 
* ECDIS settings are appropriate to the ship's dimensions and area of operations (e.g. cross track error, anti-grounding cone, safety depth and contour). 

DATA APPEARANCE - Updated 26 Jan 2021

Australian published ENCS contain either the same or greater levels of detail than the corresponding paper chart.  In particular ENCs may contain 1-metre contours that are not available in the paper chart.  This will enable the full benefits of ECDIS and particularly its safety depth alarm function to be used most effectively.  
The geographical limits of individual ENCs are likely to differ from the geographical limits of corresponding paper charts.  To interpret geographical locations of AHO published ENC from their file numbers, see the Guide to the AHO’s ENC Numbering System fact sheet at https://www.hydro.gov.au/factsheets/factsheets.htm

UPDATES - Updated 9 April 2020 

Updates for AU, PG and SB ENCs published by the Australian Hydrographic Office are issued weekly. They include all Permanent, Temporary and Preliminary Updates.  For updates to Australian Hydrographic Publications (nautical publications), and any Paper Nautical Charts used as a backup to ECDIS, mariners should consult the fortnightly Australian Notices to Mariners available via the AHO website at www.hydro.gov.au. 

IMPORTANT NOTICE - Updated 9 April 2020 

All AU, PG and SB ENCs published by the Australian Hydrographic Office include all significant hydrographic information assessed as suitable for use (including data which may be of known or unknown quality) at date of compilation.  Significant changes are promulgated via ENC Update or ENC New Edition as appropriate, as well as by Australian Notices to Mariners.   All reasonable efforts have been made to ensure the accuracy and completeness of the information, including third party information, incorporated in this product.   The AHO regards third parties from which it receives information as reliable, however the AHO cannot verify all such information and errors may therefore exist.   Where possible, indications regarding the reliability of information are noted or symbolised in the product.   Users should familiarise themselves with the meaning of any notes or symbols, including those described in AHP20 Mariners Handbook for Australian Waters.   The AHO does not accept liability for errors in third party information or the inappropriate use of Australian published ENCs. 

COPYRIGHT – Updated 9 April 2020 

Certain hydrographic information in this product is Copyright Commonwealth of Australia; and is used under licence with the permission of the Australian Hydrographic Office.  All rights reserved.  Apart from the uses permitted to the licensee under the user licence agreement, the information may not be copied, reproduced, translated or reduced to any electronic medium or machine readable form, in whole or part, without the prior written consent of the Australian Hydrographic Office. 

---------------------

BD (Bangladesh) - Added 18 June 2020
Mariners should be aware that the Bangladesh Navy Hydrographic and Oceanographic Centre promulgates navigational information through its website www.bnhoc.navy.mil.bd.  This information are not always intended to correct BNHD products but may be significant to navigation.

---------------------

BE (Belgium) - Added 18 June 2020
Belgium ENCs are kept updated by issuing updates or new editions, at least in line with the fortnightly Belgium Notices to Mariners (including the Temporary and Preliminary Notices).

---------------------

CA (Canada) - Added 10 Dec 2020
The use of Canadian ENCs is subject to the Canadian End User Licence Agreement. See: www.charts.gc.ca/copyright-droitdauteur/eula-aluf-eng.html

---------------------

CU (Cuba) (Added 18-June-2020)

Known Issue: It has been identified that the Cuban HO (ONHG) are having technical issues with their software which is resulting in unusual Update application date values (UADT). IC-ENC can confirm that the content is being updated for safety related changes

---------------------

C1 (China) - IMPORTANT INFORMATION IN SECTION 1 NOTICES TO MARINERS

Mariners should be aware that the China Navy Hydrographic Office (CNHO) announces navigational information within Section 1 of their Weekly Notices to Mariners publication. This information is not intended to correct CNHO products but may be significant to navigation. CNHO (C1) ENCs are not updated for this information, which may include changes to routeing measures and port developments.

---------------------

D2 (Faroe Islands) - Updated 31 May 2023

Known Issue: It has been identified that some ECDIS which have not been updated to the latest version of S-62 will flag that there is no ENC data available. ENCs coded by IC-ENC have passed S-58 validation checks, the D2 code was added to S-62 in June 2021. When navigating on Faroese coded ENCs and this issue is experienced, mariners are advised to contact their ECDIS provider and update their systems. 

---------------------
FI (FINLAND) - Added 11 Jun 2020

According to the SOLAS convention (IMO Marine Safety Committee Resolution 99(73) which came 
into force July 1st 2002) this official Electronic Navigational Chart (ENC) produced by the Finnish 
Transport Agency (FTA) can be used as a primary chart when the mariner is using a type approved ECDIS system.

The Finnish Transport Agency is only liable for the navigational data covering the authorised 
fairways.

This ENC is protected by copyright and it may not be reproduced without the prior written consent of 
the Finnish Transport Agency.

Topmarks shown on buoy symbols in the ENC do not exist on the actual buoys.

Buoys and beacons marking the main fairways are equipped with radar reflectors.

All feedback is very welcome to the address below. 

point of contact:

Finnish Transport and Communications Agency Traficom
Hydrographic Office
P.O. BOX 320
FI-00059 TRAFICOM
FINLAND


Tel. +358 29 534 5000


e-mail copyright@traficom.fi

Importance notice about the Saimaa deep-water route

The maximum authorised draught in the Saimaa deep-water route is 4.35m. According to IHO standard S-57, which determines the data content and technical implementation of ENC cells, the depth of a channel can be expressed with one decimal digit only. Therefore ENC cells show the maximum authorised draught of the route as 4.3m although the actual draught is 4.35m.
For further information on the Saimaa area channels and their depths please contact the Finnish Transport Agency.

Contact information:
Finnish Transport Agency 
Lappeenranta Office
Raatimiehenkatu 23
53100 Lappeenranta
Phone +358 295 34 3000

---------------------

FR (France) - Added 11 Jun 2020
Les ENC du service hydrographique et océanographique de la marine (SHOM), numérotées FRXXXXXX.YYY, sont dérivées des cartes marines papier et archives du SHOM. 
Elles sont conçues pour être utilisées conjointement avec d'autres documents publiés par le SHOM : Le Guide du Navigateur (en particulier le Volume 1 pour les informations concernant les cartes marines, leur précision et leurs limitations), les Instructions Nautiques, les Livres des Feux et Signaux de brume, les Ouvrages de radiosignaux, les annuaires des marées, les atlas de courants de marée. 
Le Groupe d'Avis aux Navigateurs permet la tenue à jour de ces documents. 
Utilisez toujours l'ENC ou la carte marine papier appropriée la plus détaillée et tenez à jour les ENC, les cartes marine papiers et les ouvrages nautiques en votre possession.
Pour toute demande d'information ou remarque relative au contenu des ENC du SHOM, vous pouvez vous adresser à : SHOM, 13 rue du Chatellier - CS 92803 - 29228 BREST CEDEX 2, FRANCE ou à cartesel@shom.fr.

Ce produit contient des éléments soumis au copyright, provenant du SHOM ou d'autres organismes. Les demandes de reproduction doivent être adressées à : SHOM, 13 rue du Chatellier - CS 92803 - 29228 BREST CEDEX 2, FRANCE.

The ENC produced by the French Hydrographic Office (SHOM), numbered FRXXXXXX.YYY, are derived from paper charts and archives of SHOM.
These ENC are to be used in conjunction with other SHOM publications : "Guide du Navigateur" (especially Volume 1 for information on the use of charts, their accuracy and limitations), sailing directions, lists of lights, lists of radiosignals, tide tables, and tidal streams atlases. 
The Notices to Mariners ("Groupe d'Avis aux Navigateurs") allows the updating of these documents.
Always use the largest scale appropriate ENC or paper chart and keep your ENCs, paper charts and publications updated.
For further information or comments about the content of French ENCs, please apply to : SHOM, 13 rue du Chatellier - CS 92803 - 29228 BREST CEDEX 2, FRANCE or to cartesel@shom.fr.

French ENCs include information protected by copyright and property of SHOM or other organisations. Reproduction consent shall be asked for to : SHOM, 13 rue du Chatellier - CS 92803 - 29228 BREST CEDEX 2, FRANCE.

---------------------

GB (United Kingdom) - Updated 15 April 2021

© Crown Copyright 2021. All Rights Reserved. This product is protected by Crown copyright and international copyright law. It is derived from Crown copyright information and copyright information published by other organisations. No part of this product may be reproduced either in whole or in part, except as specifically required by the host system, without the prior permission of the copyright holders. The product may not be sold, hired, stored in a retrieval system, or transmitted in any form or by any means, electronic, mechanical, photocopying, recording or otherwise without the prior permission of the copyright holders. Applications for the copyright holders' permission to reproduce any part of the product should be directed in the first instance to the Licensing Manager, UK Hydrographic Office, Taunton, Somerset TA1 2DN, United Kingdom.

While the UKHO has made all reasonable efforts to ensure the ENC data supplied is accurate, it may not always be complete, up to date or positioned to modern surveying standards and therefore no warranty can be given as to its accuracy. The mariner must be the final judge of the reliance placed on the information given, bearing in mind the particular circumstances, the needs of safe and prudent navigation, local pilotage guidance and available navigational aids.

Updates for UKHO ENCs are issued weekly. See www.admiralty.co.uk.

NON-WGS84 COMPATIBLE DATUMS

A number of GB cells are derived from charts that cannot accurately be referred to WGS84 Datum. Mariners are warned that these differences MAY BE SIGNIFICANT TO NAVIGATION and are therefore advised to confirm GPS positions shown in the chart display using alternative navigational techniques particularly when closing the shore or navigating in the vicinity of dangers.
Such cells contain a warning encoded as the name of a Sea Area feature covering the cell: "This chart cannot be accurately referenced to WGS84 Datum; see caution message". They are intended for use with this warning continuously displayed and should not be used otherwise.

CATEGORY OF ZONE OF CONFIDENCE IN DATA (CATZOC) VALUES:
The boundaries between Quality of Data (M_QUAL) areas with different CATZOC values for certain cells have been taken from the Source Diagram (SD) on the equivalent paper chart and are therefore approximate. 

For these M_QUAL areas, the CATZOC values are based on the simplified summary provided in the SD and therefore represent the lowest categorisation assigned to any survey in each defined group of surveys.
The degree of simplification also means that it has not been possible to identify minor gaps within survey coverage and mariners should be aware that such gaps (where lower quality survey data has been used in the compilation of the chart) may exist. 

Some ENCs are based on small scale planning charts that do not have a Source Diagram. The UKHO has adopted a policy of allocating a CATZOC value of ‘D’ to all Quality of Data (M_QUAL) areas of such charts though it recognizes that many of these areas are based on higher quality data.

All interim CATZOC values will be replaced by definitive ones in due course.

---------------------

IN (India) - Added 10 Dec 2020

The updates of the ENCs will be provided on a fortnightly basis. 
Licence agreement 
This is a legal agreement between you and the Indian National Hydrographic Office, India (INHO), relating to your use of "The Indian Electronic Navigational Charts (ENC Data). By purchasing the ENC Data and/or by using the ENC Data, you agree to be bound by this Agreement. 
TERM OF AGREEMENT - This Agreement shall commence from the date of purchase and continue for the subscription period of one year unless otherwise terminated under this Agreement. Upon expiry of the first year, you have to renew your subscription for another year at such rates and on such terms and conditions to be determined by INHO at the date of renewal. 
PAYMENT - In consideration of the grant of the license and the services provided below, you agree to pay NHO/the Distributor the full purchase price of the ENC Data. 
UPDATED - Updates will be sent to you to keep you informed of the changes made to the ENC Data. In order to receive the updates, you must register with the authorized distributor(s). 
GRANT OF LICENSE - INHO grants you a non-exclusive license to use one copy of the ENC Data, which is licensed as a single product. The ENC Data is "in use" when it is loaded into the temporary memory (i.e. RAM) or installed into the permanent memory (e.g. hard disk, CD-ROM or other storage device) of a computer. 
COPYRIGHT - The ENC Data is owned by INHO and is protected by Government of India copyright laws, international treaty provisions, and all other applicable national laws. INHO reserves all rights not expressly granted. You may not rent, lease, market, distribute or transfer the ENC Data in part or full or any such copies to others or electronically transfer the ENC Data over a network. You may not reverse engineer, de-compile, disassemble or otherwise reduce the ENC Data to a human perceivable form for distribution to any third party. You may not modify, adapt the ENC Data or create derivative works based on the ENC Data or the accompanying written materials. Except where permitted under this License, the reproduction of the entire data comprised in the ENC Data in any form is expressly forbidden. You may be held legally responsible for any copyright infringement, which is caused or encouraged by your failure to abide by the terms of this License. You may not copy the accompanying written materials. 
VARIATION OF TERMS - INHO reserves the right to vary, alter or modify these terms and conditions from time to time and every renewal of subscription shall be subject to the new terms as may be varied, altered or modified at the date of renewal. 
LIMITED WARRANTY - INHO warrants that the ENC Data will perform substantially in accordance with the accompanying written materials for the period of license from the date of receipt when properly installed on an IMO compliant ECDIS. INHO does not warrant that the operation of the ENC Data will meet your requirements or operate free from error. INHO disclaims all other warranties, either express or implied, including but not limited to implied warranties or merchantability and fitness for a particular purpose or the accuracy of the contents, with respect to the ENC Data and the accompanying written materials.   
CUSTOMER REMEDIES - INHO's entire liability and your exclusive remedy shall be, at INHO's sole 
discretion, either the refund of the purchase price paid or the replacement of the ENC Data that does not meet INHO's limited warranty, upon the return of the ENC Data to INHO with a copy of your receipt within the warranty period. The limited warranty is void if failure of the ENC Data has resulted from accident, abuse or misapplication. Any replacement ENC Data will be warranted for the remainder of the original warranty period or 30 days, whichever is longer. 
NO LIABILITY FOR CONSEQUENTIAL DAMAGE - In no event shall INHO or its suppliers be liable for any damage whatsoever (including, without limitation, indirect, special, incidental, economic or consequential damages) arising out of the use of or inability to use the ENC Data or reliance on the ENC Data, even if NHO has been advised of the possibility of such damage. In any case, INHO's entire liability whether under this Agreement or in tort (including negligence), product liability or otherwise shall be limited to the amount actually paid by you. 
TERMINATION - This Agreement shall continue in force until terminated in any of the following ways : 
(a) destruction of the ENC Data; 
(b) automatically if you fail to comply with any term or condition of this Agreement; and
(c) upon expiry of the subscription period or any renewal thereof. 
Upon such termination, you agree to return the ENC Data to INHO. The right of termination shall be in addition to any other remedies available to INHO. 
GENERAL - This Agreement shall be governed and interpreted in accordance with the laws of the Republic of INDIA. Failure or delay in enforcing any right or provision of this Agreement shall not be deemed at waiver of such provision or right in respect of any subsequent breach or a continuance of any existing breach. 
The ENCs on this Media are encrypted and can be accessed by the specific Electronic Chart Display and Information System (ECDIS) on procuring the license for the requisite ENCs. 

---------------------

IT (Italy) - Added 18 Jun 2020

GENERAL WARNING - Only official ENCs and nautical publications should be used, provided by authorized distributors, as listed in the Italian HO website. The IIM is not responsible for Nautical Charts, Electronic Charts and Publications that have not been acquired at or through its authorized distribution channels. 
All Italian charts and ENCs have to be used in conjunction with other Italian Nautical Publications, such as Introduction to Italian Sailing Directions (I.I. 3146), Italian Sailing Directions (I.I. 3201 - 3208, 8 volumes), Italian List of Lights and Fog Signals (I.I. 3134), List of Radio signals (I.I. 3128 and I.I. 3130) and other IIM publications.
Attention is drawn to the statement in A.N. n° 16 of the Introduction to Italian Sailing Directions (I.I. 3146) concerning the use of navigational information provided by the IIM. This includes the following caution which applies to all charts, paper and electronic: "Finally, mariners must never forget that a nautical chart can never be perfectly coincident with reality, being only one of its possible representations; they must always consider the possible presence of particular dangers, prescriptions and regulations, especially near the coast, and therefore consult all the relevant Nautical Documentation as well as general and local regulations.
An incorrect or improper use of Nautical Documentation does not guarantee the adequate and necessary levels of safety, especially when sailing near the coast".

COPYRIGHT - All the Italian cells included in the present exchange set are Official Nautical Charts as per Italian Law 2.2.1960. They include information protected by Copyright. Reproduction is prohibited without the Istituto Idrografico della Marina’s explicit consent.

UPDATES - Updates for IT ENCs are generally issued every two weeks. Updates are issued for all Permanent Chart-Correcting and Preliminary Notices to Mariners. Mariners should consult the Italian Notices to Mariners (pdf format) at http://www.marina.difesa.it/conosciamoci/organizzazione/comandienti/scientifici/idrografico/Pagine/home.aspx

ENC DATUMS - A number of IT cells are derived from paper charts that cannot accurately be referred to WGS84 Datum, and refer to a local datum (Bessel, Rome 1940) or a European Datum (1950). Positions obtained from Global Navigation Satellite Systems, such as GPS, are normally referred to WGS84 Datum. The differences between satellite-derived positions and positions on these cells cannot be accurately determined; the estimated differences are detailed in the HORDAT & SHIPAM attributes of M_HOPA. Old cells contain a "Chart Accuracy" Caution warning Mariners about accuracy issues due to very old data or different Datum; these can be significant particularly when closing the shore or navigating in the vicinity of dangers. In these cells any case, the quality of data specified in the M_QUAL parameters is set considering also this kind of issues.

M_QUAL - The boundaries between Quality of Data (M_QUAL) areas with different CATZOC values for certain cells have been taken from the Source Data Diagram (SDD) on the equivalent paper chart and they should therefore be considered as approximate. This may be reported in a specific Caution.

---------------------

KR (Republic of Korea)

Korea has requested that ENC KR679A95 (Eastern Part of Mukho Hang) be removed from sale. Korea have advised that this ENC is still valid for use by existing holders and will continue to be updated until the end of 2021, when all licences will have expired.

This ENC is therefore no longer available to order or renew. It will remain in the PRODUCTS.TXT file and therefore the polygon limits may display on ECDIS, even though it cannot be purchased. To remain compliant, vessels should routinely check their planned routes to make sure that they have purchased all other ENCs under their track. 

---------------------

NZ (New Zealand)  - Added 18 Jun 2020

NZ (NEW ZEALAND) ENCs

Published by the New Zealand Hydrographic Authority
Toitu Te Whenua Land Information New Zealand (LINZ)
155 The Terrace
PO Box 5501
Wellington 6145
New Zealand

Tel: +64 4 460 0110
General enquiry Tel: 0800 665 463
Fax +64 4 460 0161
Email customersupport@linz.govt.nz
www.linz.govt.nz

DATA APPEARANCE
QUALITY OF DATA. The M_QUAL object with a CATZOC attribute is included in New Zealand ENCs as an overlay. This may affect the appearance of the New Zealand ENCs as the ZOC polygons may overlap the LNDARE.
MINIMUM SCALE. The SCAMIN step values for object and attribute combinations have been applied to New Zealand ENCs following international standards.

UPDATES
Updates for New Zealand ENCs are issued in line with the fortnightly electronic New Zealand Notices to Mariners booklet. New Zealand ENC updates are issued for all Permanent, Temporary and Preliminary Notices to Mariners. New Zealand Notices to Mariners can be downloaded from the LINZ website at www.linz.govt.nz or subscribe online at the LINZ website to receive fortnightly Notices to Mariners via email. 

IMPORTANT NOTICE
The information contained in this publication has been either produced by, or supplied to, LINZ and was current as at the date of compilation. Significant hydrographic information is updated by a fortnightly Notices to Mariners service to which all mariners are encouraged to subscribe. All reasonable efforts have been made to ensure accuracy and completeness of the contents of New Zealand ENCs. LINZ regards third parties from which it receives information as reliable, however LINZ cannot verify all such information and errors may therefore exist.

DISCLAIMER
LINZ accepts no liability for any direct, indirect, consequential or incidental damages that result from any errors in the New Zealand ENCs whether due to LINZ or a third party, or that arise from the use, or misuse, of the information contained in New Zealand ENCs.

COPYRIGHT
New Zealand ENCs are owned by LINZ and protected under the NZ Copyright Act 1994 and international convention. All rights reserved. Apart from the uses permitted to the licensee under the user licence agreement, the information may not be copied, reproduced, translated or reduced to any electronic medium or machine readable form, in whole or part, without the prior written consent of LINZ.

---------------------

SG (Singapore)  - Added 10 Dec 2020
© MARITIME AND PORT AUTHORITY OF SINGAPORE, SINGAPORE 
All rights reserved. No part of the ENC data produced by the Singapore Maritime and Port Authority (MPA) may be reproduced, stored in a retrieval system, or transmitted, in any form or by any means, electronic, mechanical, photocopying, recording or other-wise, without the prior written permission of Maritime and Port Authority of Singapore. 
Limited warranty 
With respect to any end-user of ENC data produced by the Singapore Maritime and Port Authority (MPA) (the MPA Data) neither the UKHO nor the MPA warrants that the MPA Data will meet your requirements or be free from error. To the fullest extent permitted under applicable law, in respect of MPA Data the UKHO and the MPA hereby exclude all other warranties, whether express or implied, including but not limited to implied warranties of merchantable quality and fitness for purpose. The UKHO and the MPA offer no warranty as to the accuracy of MPA Data or any other materials associated with it and supplied pursuant to this Agreement.  
No liability for consequential damage 
In no event shall the UKHO or the MPA (even if advised of the possibility) be liable for any damage whatsoever (including but not limited to indirect, special, incidental, economic or consequential damage (including but not limited to loss of profits or environmental damage) and howsoever caused arising out of or in connection with your use, misuse or non-use of the MPA Data or arising out of or in connection with your reliance on MPA Data. 

---------------------

UA (Ukraine) - Added 18 Mar 2022

Owing to insufficient information, it is not always possible to ensure that Ukraine (UA) ENCs are completely up to date for new dangers or changes to aids to navigation. 
Mariners are therefore advised to exercise particular caution when navigating in Ukrainian waters.

---------------------

US (United States of America) - Added 18 Jun 2020

The NOAA electronic navigational chart (NOAA ENC®) files that may be downloaded from the National Oceanic and Atmospheric Administration (NOAA) are official products. Official ENCs are suitable for navigation under the following regulations and policies, and in the following circumstances:

1. On May 2, 2001, the United States Coast Guard amended the chart carriage regulations to permit government vessels to use electronic charting and navigation systems in lieu of paper charts.

2. On August 15, 2002, the United States Coast Guard issued a statement of policy to bring its regulations into agreement with SOLAS Chapter V (as amended by the changes taking effect July 1, 2002). The policy statement said that, if a ship has an approved ECDIS installed according to SOLAS, Chapter V, the ECDIS will be considered as meeting the ship's nautical chart carriage regulations. The ships that are affected are:

a. U.S.-flagged ships of 150 or more gross tons that engage on international voyages.

b. U.S.-flagged ships certificated solely for service on the Great Lakes and the St. Lawrence River.

c. Foreign-flag ships to which SOLAS, Chapter V, applies that are operating on the navigable waters of the United States. 

This policy is not applicable to U.S.-flag ships engaged only on domestic voyages. These ships must continue to comply with existing navigation equipment requirements until the chart carriage regulations are formally amended.

The NOAA ENC® files available from this site are updated for Notices to Mariners on at least a monthly basis. Users are encouraged to check the site frequently and download the latest versions of the files. Updates are currently available as incremental updates (also called ENC Revision or "ER" files).

Copying, reproduction of the NOAA ENC®s to any other server or location for further reproduction or redistribution is discouraged unless the following guidelines are followed: 1) the NOAA ENC®s and supplementary files including the CATALOG.031 and README.TXT are included in their entirety, 2) this disclaimer is displayed to and accepted by anyone who accesses the NOAA ENC®s, and 3) a reference to this Web site is included so that anyone accessing the NOAA ENC®s is advised of where they originated. 

If these NOAA ENC®s are incorporated into any other product in a form other than as provided by NOAA, the producer of that product assumes full liability and must adhere to conditions 2 and 3 described in the paragraph above concerning copying or reproduction.

---------------------

UY (Uruguay) - Added 18 Jun 2020

Companies, Port Works Companies, and any other person connected with maritime activities, report any alteration or irregularity that may affect the safety of navigation by:

Oceanographic, Hydrographic and Meteorological Office of the Navy.
Address: 980 Capurro St.- Montevideo - URUGUAY - PO Box: 1381 - Postal Code: 11700
Telephones: (+598) 2307 1777 / 2309 3775 / 2309 3861 - Tel./Fax: (+598) 2309 9220
            (+598) 99679071 Real Time Tide in Montevideo Bay

Email: sohma@armada.mil.uy or to the nearest Maritime Authority.
NOTE: See website of SOHMA thorugh the website of the Uruguayan Navy: www.armada.mil.uy/sohma/web/index.html

"SAIL WITH SAFETY"
Get your charts and nautical publications AT OUR OFFICE OR OUR AUTHORIZED SALES AGENTS.
The Oceanographic, Hydrographic and Meteorological Office of the Navy of the Eastern Republic of Uruguay, keeps its cartography permanently updated, so it recommends its use in domestic and foreign vessels when navigating in Uruguayan waters.
The SOHMA is not responsible for Nautical Charts and Publications that have not been acquired at its local sales, or through their authorized agents or the Maritime Authority of the area.


Se solicita a los Navegantes, Prácticos, Organismos del Estado, Entes Autónomos, Compañías Navieras, Empresas de Obras Portuarias y toda otra persona relacionada con el ámbito marítimo, comuniquen cualquier alteración o irregularidad que pueda afectar la seguridad de la navegación al: 

Servicio de Oceanografía, Hidrografía y Meteorología de la Armada.
Dirección: Capurro 980 - Montevideo - URUGUAY - Casilla de Correos: 1381 - Código Postal: 11700
Teléfonos: (+598) 2307 1777 / 2309 3775 / 2309 3861 - Tel./Fax: (598) 2309 9220
           (+598) 99679071 Marea en tiempo real BAHIA DE MONTEVIDEO

E-mail: sohma@armada.mil.uy ó a la Autoridad Marítima más próxima.
NOTA: Consulte el sitio web del SOHMA ingresando desde el sitio de la Armada Nacional: www.armada.mil.uy/sohma/web/index.html


"NAVEGUEMOS SEGUROS"
ADQUIERA SUS CARTAS Y PUBLICACIONES NÁUTICAS EN NUESTRA OFICINA CENTRAL O EN NUESTROS AGENTES DE VENTAS AUTORIZADOS.
El Servicio de Oceanografía, Hidrografía y Meteorología de la Armada de la República Oriental del Uruguay, mantiene su cartografía permanentemente actualizada, por lo que recomienda su empleo en Buques Nacionales y Extranjeros cuando naveguen aguas uruguayas.
El SOHMA no se responsabiliza por Cartas Náuticas y Publicaciones que no hayan sido adquiridas en su local de ventas, o a través de sus Agentes autorizados o de la Autoridad Marítima de la zona.

---------------------

1U (Taiwan)

In addition to official GB ENCs, extra information in the form of datasets issued by Taiwan, has been included in AVCS to assist with safe navigation and for use entirely at the vessel's risk. For vessels navigating in coastal waters or entering ports of Taiwan, local authorities may require this data to be used on ECDIS to meet their chart carriage requirements.

Because the producer is not listed as official by the International Hydrographic Office, the Taiwan datasets may show up in ECDIS displays as "non-ENC" or "unofficial". For compliance purposes the local authorities have stated that the indication can be ignored.

=============

SECTION 3 - WITHDRAWN CELLS - Updated 11-Jun-2020
---------------------

WITHDRAWN ENCs

ENCs may be withdrawn from ADMIRALTY services without the issue of a cancellation update because of safety concerns about their content or overlap with other data.  The withdrawal of ENCs is communicated in this README.TXT file and in Section VIII of the ADMIRALTY Notices to Mariners Bulletin until all relevant permits have expired from AVCS licences.

The following ENCs are currently withdrawn from ADMIRALTY services:

None

If withdrawn ENCs are installed in your ECDIS, you are advised to use them with caution and refer to the appropriate paper chart coverage as they may be out of date.

=============


SECTION 4 - MISCELLANEOUS INFORMATION - Updated 07-May-2020
----------------------------

COPYRIGHT:

(C) Crown Copyright 2020. All rights reserved. This product is protected by Crown copyright. It is derived from Crown copyright information and copyright information published by other organisations. No part of this product may be reproduced either in whole or in part, except as specifically required by the host system, without the prior permission of the copyright holders. The product may not be sold, hired, stored in a retrieval system, or transmitted in any form or by any means, electronic, mechanical, photocopying, recording or otherwise without the prior permission of the copyright holders. Applications for the copyright holders' permission to reproduce any part of the product should be directed in the first instance to the Licensing Manager, UK Hydrographic Office, Taunton, Somerset TA1 2DN, United Kingdom.

---------------------

For further information on the use of ENC data, please refer to the User Guide and NP100.
 
For further information on the terms and conditions of supply of ENC data, please consult the ADMIRALTY End User Licence Agreement which is available at: admiralty.co.uk/eula.

If you have any queries related to this service please contact:

UKHO Customer Services
United Kingdom Hydrographic Office 
Admiralty Way
Taunton
Somerset TA1 2DN
United Kingdom
Tel:	+44 (0) 1823 484444
E-mail: CustomerServices@ukho.gov.uk

