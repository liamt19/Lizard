
ifndef EXE
    EXE = Lizard
endif

ifndef CC
    CC = dotnet
endif


release:
	dotnet publish . --self-contained -v quiet --property WarningLevel=0 -o ./ -c Release -p:DefineConstants="$(DefineConstants)PEXT" -p:AssemblyName=$(EXE) -p:DebugType=embedded
	del /Q .\bin\Release\*
	rmdir .\bin\Release
