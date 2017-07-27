#!/usr/bin/env bash

# This file is a part of Companion Cube project.
#
# Copyright (C) 2018-2021 Emzi0767
# 
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.
# 
# You should have received a copy of the GNU Affero General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.

# Infinite loop
while true
do
	# Run lavalink
	java -jar Lavalink.jar

	# Grab its exit code
	exitcode=$?

	# Was the exit code 0 (clean exit)?
	if [ "$exitcode" == "0" ]
	then
		# It was, so let's assume that user wanted to quit
		echo "Lavalink exited cleanly, quitting"
		break
	fi

	# Exit wasn't clean, possibly a crash
	# Sleep for a bit then restart
	echo "Lavalink exited non-cleanly, waiting 5s then restarting..."
	sleep 5
done