#!/bin/bash

set -e

rm -rf BalancedDuoSentry/bin BalancedDuoSentry/obj

dotnet build
