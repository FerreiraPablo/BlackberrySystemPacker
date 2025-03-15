# Blackberry Signed Image Patcher
![Build](https://github.com/FerreiraPablo/BlackberrySystemPacker/actions/workflows/dotnet.yml/badge.svg)

A .NET 8 Console application for patching signed Blackberry firmware images.

## Overview

This tool allows you to modify signed Blackberry firmware images without ignoring their signature validation. It provides a command-line interface for common patching operations on Blackberry system files, a proof of concept for the modification of images with QNX6 filesystems.

Developed by Pablo Ferreira for educational purposes, this tool is not affiliated with Blackberry or Research In Motion.

Source code published for the first time on 3/3/2025

## Features
- Patch signed Blackberry firmware images
- Preserve image signatures while modifying content
- Command-line interface for batch processing
- Support for common Blackberry firmware formats

## Requirements

- .NET 8.0 SDK or Runtime
- Windows, macOS, or Linux operating system

## Installation

### Option 1: Download the release

Download the latest release from the [Releases](https://github.com/FerreiraPablo/BlackberrySystemPacker/releases) page.

### Option 2: Build from source

```bash
git clone https://github.com/FerreiraPablo/BlackberrySystemPacker.git
cd BlackberrySystemPacker
dotnet build -c Release
```

## Usage

```
BlackberrySystemPacker [procedure] [options]
```

### Commands

- `AUTOPATCH`: Patches and image and removes unnecesary obsolete stuff.
- `EDIT`: Edit an image and add or remove files.

Run with `HELP` for detailed command options.

## Examples

```bash
# Patch a firmware image
bbsignedpatcher AUTOPATCH --os <path_to_os_file> --output <output_directory>

bbsignedpatcher AUTOPATCH --os <path_to_os_file> --radio <path_to_radio_file> --output <output_directory> --autoloader

bbsignedpatcher EDIT --os <path_to_os_file> --output <output_directory> --workspace <workspace_directory>

bbsignedpatcher EDIT --os <path_to_os_file> --radio <path_to_radio_file> --output <output_directory> --workspace <workspace_directory> --autoloader

bbsignedpatcher HELP
```

## License

[MIT License](LICENSE)

## Disclaimer

This tool is provided for educational and development purposes only, and is completely unrelated to Research In Motion or Blackberry as a brand. Use it responsibly and respect intellectual property rights.
