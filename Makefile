
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
#	-p:EVALFILE=$(EVALFILE)		Path to a network to be loaded. Note the file is NOT embedded, so it can't be moved or the binary will fail to load it.
#								This should probably be an absolute path.
BUILD_OPTS := --self-contained -v quiet --property WarningLevel=0 -o ./ -c Release -p:AssemblyName=$(EXE) -p:DebugType=embedded -p:EVALFILE=$(EVALFILE)


#	-p:PublishAOT=true				Actually enables AOT
#	-p:PublishSingleFile=false		AOT is incompatible with single file publishing
#	-p:IS_AOT=true					Sets a variable during runtime signalling AOT is enabled, same to how EVALFILE works.
AOT_OPTS = -p:PublishAOT=true -p:PublishSingleFile=false -p:IS_AOT=true


OPT_512 = -p:DefineConstants="$(DefineConstants)AVX_512"

AVX_OR_NOT = 

GET_COMP_INFO = $(shell echo | $(EXE) compiler)


define CHK
	ifneq ($(findstring Avx512, $(GET_COMP_INFO)),)
		$(eval AVX_OR_NOT=true)
	endif
endef


default_build:
	dotnet publish . $(BUILD_OPTS)
	-rmdir /s /q .\bin\Release
	$(MAKE) check512


check512:
	$(eval $(call CHK))
	$(MAKE) release


#	Try building the non-AOT version first, and then try to build the AOT version if possible.
#   This recipe should always work, but AOT requires some additional setup so that recipe may fail.
release:
	dotnet publish . $(BUILD_OPTS) $(AVX_OR_NOT)
	-rmdir /s /q .\bin\Release
	$(MAKE) aot


#	This will/might only succeed if you have the right toolchain
aot:
	-dotnet publish . $(BUILD_OPTS) $(AOT_OPTS) $(AVX_OR_NOT)
	-rmdir /s /q .\bin\Release\native
	-rmdir /s /q .\bin\Release
	-del .\Lizard.pdb
