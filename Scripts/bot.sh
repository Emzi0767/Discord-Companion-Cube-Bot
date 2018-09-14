#!/usr/bin/env bash

# This file is a part of Companion Cube project.
#
# Copyright 2018 Emzi0767
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Infinite loop
while true
do
	# Run the bot
	./Emzi0767.CompanionCube

	# Grab its exit code
	exitcode=$?

	# Was the exit code 0 (clean exit)?
	if [ "$exitcode" == "0" ]
	then
		# It was, so let's assume that user wanted to quit
		echo "Bot exited cleanly, quitting"
		break
	fi

	# Exit wasn't clean, possibly a crash
	# Sleep for a bit then restart
	echo "Bot exited non-cleanly, waiting 5s then restarting..."
	sleep 5
done