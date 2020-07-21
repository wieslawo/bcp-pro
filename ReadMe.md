# bcp_pro
Improved bcp command line tool

Microsoft bcp tool for import/export data from MS SQL has few constraints.

First problem is when you try to import csv file with data in daouble quotation marks. bcp doesn't allow such file, when you have such file from other system for example from mongoexport tool you have problem.

Second problem is when you export to csv file, when you have in some column data with comma, also problem. You can use different separator but when other system required only comma there is problem.

I needed command line tool for export and import csv file from MS SQL so I decided to write such tool in .NET Core.



## You can use this in similar like original bcp, for example:

bcp_pro dbo.someTable -o D:\temp\SomeExport.csv -S . -D Test -T

bcp_pro dbo.someTable -i D:\temp\SomeImport.csv -S server -D db -U admin -P xxxx

For help

bcp_pro -h