#!/bin/sh
set -e

echo "Running startup tasks..."

# pre-start commands
dotnet Chatbot.dll migrate

echo "Starting application..."

exec "$@"