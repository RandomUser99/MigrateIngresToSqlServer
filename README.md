# Migrate Ingres to SQL Server

## Overview

The purpose of this solution was to facilitate the migration of a databse running in Ingres 2.6 running in a *nix environment over to a MS SQL Server database. This process uses the ```copydb``` utility that comes with Ingres to facilitate extracting the schema and data from the source database.

I couldn't find any way of migrating the existing database to MSSQL server without spending money, which wasn't an option for an Ingres 2.6 database. This process may work for newer versions of Ingres, but there is no guarantee and the file formatting will likely have been changed as time has gone on. It's up to you if you want to try it on later versions.

## Extracting the data from the source Ingres database

Ingres comes with a utility called [copydb](https://docs.actian.com/ingres/10.2/index.html#page/CommandRef/copydb_Command--Copy_and_Restore_a_Database.htm):

> The copydb command creates command files containing the SQL statements required to copy and restore a database. The command creates the following two command files in the current directory:
>
>• copy.out contains SQL commands to copy all tables, views, and procedures owned by the user into files in the specified directory.
>
>• copy.in contains SQL commands to copy the files into tables, recreate views, procedures, and indexes, and perform modifications.

An example in the [docs](https://docs.actian.com/ingres/10.2/index.html#page/CommandRef%2Fcopydb_Example_on_UNIX.htm%23) shows the process to use ```copydb``` to create the copy.in and copy.out files and then use the copy.in and the ```sql``` utility to export the data to a set of files written to the filesystem.

```shell
cd /usr/mydir/backup
copydb mydb /usr/mydir/backup
sql mydb <copy.out
```

The files generated are what we can use in *MigrateIngresToSql* to parse the schema and data from the source database into an MSSQL database.

## No large file support in Ingres 2.6

A limitation I found with Ingres 2.6 was that it doesn't have large file support, so the size of the data file for each table it can create is limited to 4GiB (or 2GiB, I can't remember, let's say 4GiB for now). If you have a table in your source database with suffient data to create an output file larger than 4GiB, it will just stop writing data to the file without error or warning, and you will find you have missing rows in your destination table. I spotted the offending files in my exported files set as they had something like a 4GiB file size and they were all a very specific size (which I cannot remember).

I luckily found a ```comp.databases.ingres``` usenet post via google groups where someone kindly explained how to work around this by piping the data into an output buffer which then writes to a file, bypassing the 4GiB file limitation. You can find that post [here](https://groups.google.com/g/comp.databases.ingres/c/t7OWVQzq_as/m/l_20X6bWTkAJ), but I will post the pertitnent info here in case it disappears for some reason:

> Named pipes.
> 1) create a named pipe (aka fifo)
>
> *mkfifo myfifo*
>
> 2) create a background process to read from it
>
> *gzip < myfifo > file.dat.gz &*
>
> 3) edit your copy.out to write to the fifo rather than a file
>
> *copy(...*
> *...*
> *into 'myfifo'*

I created a copy of the original copy.in file and amended it to just target the individual table I wanted to export which was over the size limitation, then carried out the above steps. I only had a few offending tables, so I did these few invidividually after the main export files were created.

## Importing the data into the destination MSSQL database

The ```MigrateIngresToSql``` tool will read the copy.in file, generate an array of TableDefinition types which contain the table name, the fields name and type and the name of the psa file containg he exported data.

Each of the TableDefinions is iterated through and the associated ```<Table>.psa``` file contents will be read and a corresponding [```System.Data.Datatable```](https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable?view=netframework-4.7.2) will be created, mapping as closely as possible the source and destination database types, but fall back to ```string``` (which will translate to ```nvarchar``` in MSSQL Server) if a corresponding type cannot be found.

By default, no tables will be created or data will be saved to the destination database unless you set the following to true in ```Program.cs```, it will just go through the process of parsing the expored files. Setting it to true will create the destination tables and import the data:

```csharp
private const bool SaveToDatabase = false;
```

If ```SaveToDatabase``` is set to ```false```, the source files are parsed but no data is written. A log file will be written out to the location specified in the ```app.config``` in csv format.

If ```SaveToDatabase``` is set to ```true```, the table will be created in the database and write the data to the destination database using [```SqlBulkCopy.WriteToServerAsync()```](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy.writetoserverasync?view=netframework-4.7.2) before moving on to the next TableDefinition.

Any subsequent runs of the tool will not create any tables which already exist.
