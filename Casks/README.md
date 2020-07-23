This directory contains all the modified casks needed by the project. These casks make sure that the correct version
of the dependencies can be installed without using a specific version of homebrew

# Usage

In order to use the casks in the repo you need to execute:

```bash
brew tap xamarin/binding-tools-for-swift git@github.com:xamarin/binding-tools-for-swift.git
```

It is important to use the git url else brew will default to https and will ask for the github credentials. Once the tap
has been added you can use brew as you normally do:

```bash
brew cask install cmake-btfs
```

# Casks

The following casks are available:

## cmake-btfs

cmake version required for the project. Once installed the system version will uses the provided version of the cask. If you need to revert this change you can reinstall the cmake cask from brew and do:

```bash
brew link --overwrite cmake
```

