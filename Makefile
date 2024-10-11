
ifndef EXE
	EXE = Lizard
endif

ifndef OUT_PATH
	OUT_PATH = .\\
endif

ifeq ($(OS),Windows_NT) 
	BINARY_SUFFIX = .exe
	PDB_SUFF = pdb

	RENAME_CMD = -ren
	RM_FILE_CMD = del
	RM_FOLDER_CMD = rmdir /s /q
else
	PDB_SUFF = dbg
	BINARY_SUFFIX = 

	RENAME_CMD = mv
	RM_FILE_CMD = rm
	RM_FOLDER_CMD = rm -rf
endif

FULL_EXE_PATH = $(EXE)$(BINARY_SUFFIX)
RM_PDB = -$(RM_FILE_CMD) $(EXE).$(PDB_SUFF)
RM_BLD_FOLDER = -cd bin && $(RM_FOLDER_CMD) Release && cd ..
RM_OBJ_FOLDER = -$(RM_FOLDER_CMD) obj

INST_SET = native


# Macos doesn't seem to like this parameter and the GenerateBundle task fails during building.
OUT_DIR = -o ./
ifneq ($(OS),Windows_NT)
	UNAME_S := $(shell uname -s)
	ifeq ($(UNAME_S),Darwin)
		OUT_DIR =
	endif
	UNAME_P := $(shell uname -p)
	ifneq ($(filter arm%,$(UNAME_P)),)
		OUT_DIR =
	endif
endif

OUT_DIR = -o ./

#  DEFAULT_NET trick here is based off of Stormphrax (https://github.com/Ciekce/Stormphrax/blob/main/Makefile)
#  It uses Github releases so that the size of this main repo doesn't get too large.
ifeq ($(UNAME_S),Darwin)
	DEFAULT_NET := $(shell cat network.txt)
else
	DEFAULT_NET := $(file < network.txt)
endif

ifndef EVALFILE
	EVALFILE = $(DEFAULT_NET).bin
	NO_EVALFILE_SET = true
endif


#  self-contained              .NET Core won't need to be installed to run the binary
#  -v quiet                    Silences CS#### warnings during building (e.g. "CS0162: Unreachable code detected")
#  -p:WarningLevel=0           Silences CS#### warnings during building
#  $(OUT_DIR)                  Should be "-o ./", which outputs the binary in the current directory
#  -c Release                  Builds using the Release configuration in Lizard.csproj
#  -p:AssemblyName=$(EXE)      Renames the binary to whatever $(EXE) is.
#  -p:DebugType=embedded       Places the PDB file inside the binary
#  -p:EVALFILE=$(EVALFILE)     Path to a network to be loaded. Note the file is NOT embedded, so it can't be moved or the binary will fail to load it.
#                              This should probably be an absolute path.
BUILD_OPTS := --self-contained -v diag $(OUT_DIR) -c Release -p:AssemblyName=$(EXE) -p:DebugType=embedded -p:EVALFILE=$(EVALFILE)


#  -p:PublishAOT=true                 Actually enables AOT
#  -p:PublishSingleFile=false         AOT is incompatible with single file publishing
#  -p:IS_AOT=true                     Sets a variable during runtime signalling AOT is enabled, same to how EVALFILE works.
#  -p:IlcInstructionSet=$(INST_SET)   Instruction set to use, should be "native" if you are only running the binary on your cpu.
AOT_OPTS = -p:PublishAOT=true -p:PublishSingleFile=false -p:IS_AOT=true -p:IlcInstructionSet=$(INST_SET)


.PHONY: release
.DEFAULT_GOAL := release

ifdef NO_EVALFILE_SET
$(EVALFILE):
	$(info Downloading default network $(DEFAULT_NET).bin)
	curl -sOL https://github.com/liamt19/lizard-nets/releases/download/$(DEFAULT_NET)/$(DEFAULT_NET).bin

download-net: $(EVALFILE)
endif

#  Try building the non-AOT version first, and then try to build the AOT version if possible.
#  This recipe should always work, but AOT requires some additional setup so that recipe may fail.
release: $(EVALFILE)
	dotnet publish . $(BUILD_OPTS)


#  This will/might only succeed if you have the right toolchain
aot: $(EVALFILE)
	-dotnet publish . $(BUILD_OPTS) $(AOT_OPTS)


512: $(EVALFILE)
	dotnet publish . $(BUILD_OPTS) -p:DefineConstants="AVX512"


aot_512: $(EVALFILE)
	-dotnet publish . $(BUILD_OPTS) $(AOT_OPTS) -p:DefineConstants="AVX512"

all:
	$(MAKE) aot INST_SET=x86-x64
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)-aot-v1$(BINARY_SUFFIX)
	$(MAKE) aot INST_SET=x86-x64-v2
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)-aot-v2$(BINARY_SUFFIX)
	$(MAKE) aot INST_SET=x86-x64-v3
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)-aot-v3$(BINARY_SUFFIX)
	$(MAKE) aot_512 INST_SET=x86-x64-v4
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)-aot-v4$(BINARY_SUFFIX)
	$(MAKE) 512
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)-512$(BINARY_SUFFIX)
	$(MAKE) release
	$(RENAME_CMD) $(FULL_EXE_PATH) $(EXE)$(BINARY_SUFFIX)

clean:
	$(RM_OBJ_FOLDER)
	$(RM_BLD_FOLDER)
	$(RM_PDB)