#!/bin/bash

# Wrapper around the sharpener tool that makes it easier to use.
# Builds the .net project if necessary, reads the build from the
# stack trace to find the right binaries etc.
#
# Also handles symlinks and ~ expansion as this is easier to do from
# a bash script than by using .net.

usage()
{
  echo "Usage: $0 [-h] [-r] [-s path-component] symbols-dir stacktrace"
  echo
  echo " -h    - Show this help"
  echo " -b    - force building the sharper-crashes tool"
  echo ""
  echo " The directory symbols-dir should contain directories named <platform>-<app id>-<build number>."
  echo ""
  echo " Example:"
  echo ""
  echo " symbols/ios-com.example.myapp-1001/<dlls and pdbs for iOS build 1001>"
  echo " symbols/android-com.example.myapp-1001/<dlls and pdbs for Android build 1001>"
  echo " symbols/android-com.example.myapp-1203/<dlls and pdbs for Android build 1203>"
  echo ""
  echo " The file stacktrace should be a decorated stack trace with IL offsets and method tokens."
  echo " The first line should point to a directory name with dlls and pdbs for that build, e.g.:"
  echo ""
  echo " ios-com.example.myapp-1002"
  echo "     at CrashHere() IL_0000 T_06000012"
}

sharpener=sharpener/bin/Release/net7.0/sharpener

while getopts :hbs: option
do
    case ${option} in
        h) usage; exit 0;;
        b) rm $sharpener 2> /dev/null;;
        \?) usage; exit 2;;
    esac
done
shift $((OPTIND - 1))

if [[ $# != 2 ]]; then
    usage
    exit 2
fi

symbols="$1"
trace="$2"

if [[ ! -f $trace ]]; then
  echo "No trace found at $trace"
  exit 1
fi

build=$(head -1 "$trace" 2> /dev/null)

if [[ $build == "" ]]; then
  echo "No build info found in stack trace"
  usage
  exit 1
fi

if [[ ! -f $sharpener ]]; then
    echo Building sharpener...
    (cd sharpener || exit 1; dotnet build -c:Release > /dev/null)
fi

if [[ -L $symbols ]]; then
  symbols=$(readlink -f "$symbols" 2> /dev/null)
fi
symbols=$(realpath "$symbols" 2> /dev/null)

if [[ ! -d $symbols/$build ]]; then
  echo "No symbols directory found in $symbols/$build"
  exit 1
fi

if [[ -L $trace ]]; then
  trace=$(readlink -f "$trace" 2> /dev/null)
fi
trace=$(realpath "$trace" 2> /dev/null)

echo "Sharpening with symbols from build $build..."
$sharpener "$symbols/$build" "$trace"
