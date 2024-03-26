
ifndef EXE
	EXE = Lizard
endif

#	I had issues with $(CC) not being defined (or being defined as "cc"?), so for now this will require dotnet to be in your path
ifndef CC
	CC = dotnet
endif

#	self-contained              .NET Core won't need to be installed to run the binary
#	-v quiet                    Silences CS#### warnings during building (e.g. "CS0162: Unreachable code detected")
#	--property WarningLevel=0   Silences CS#### warnings during building
#	-o ./                       Outputs the binary in the current directory
#	-c Release                  Builds using the Release configuration in Lizard.csproj
#	-p:AssemblyName=$(EXE)      Renames the binary to whatever $(EXE) is.
#	-p:DebugType=embedded       Places the PDB file inside the binary
BUILD_OPTS = --self-contained -v quiet --property WarningLevel=0 -o ./ -c Release -p:AssemblyName=$(EXE) -p:DebugType=embedded


#	Try building the non-AOT version first, and then try to build the AOT version if possible.
#   This recipe should always work, but AOT requires some additional setup so the aot recipe may fail.
release:
	dotnet publish . $(BUILD_OPTS) -p:DefineConstants="$(DefineConstants)" -p:EVALFILE=$(EVALFILE)
	-rmdir /s /q .\bin\Release
	$(MAKE) aot


#	Replaces PublishSingleFile with PublishAOT
#	-p:DebugType=embedded apparently just... doesn't embed it? So this will delete the pdb as well.	
#	https://github.com/dotnet/sdk/issues/35798
aot:
	-dotnet publish . $(BUILD_OPTS) -p:PublishAOT=true -p:PublishSingleFile=false -p:DefineConstants="$(DefineConstants)PUBLISH_AOT" -p:EVALFILE=$(EVALFILE)
	-rmdir /s /q .\bin\Release\native
	-rmdir /s /q .\bin\Release
	-del .\Lizard.pdb