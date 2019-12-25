# bmpurge

## Usage

`bmpurge file -before [timestamp, string] -after [timestamp, string]`

**Before:** all bookmarks before this date will be erased

**After:** all bookmarks after this date will be erased

**File:** Path to the bookmarks HTML file.

**File will be overwritten without warning. Always keep a backup.**

## Examples

`bmpurge path/to/file -before "2018/01/01 12:00"` will erase all bookmarks created before 01 Jan 2018, 12:00.

`bmpurge path/to/file -after "2018/01/01 12:00"` will erase all bookmarks created after 01 Jan 2018, 12:00.

`bmpurge path/to/file -before "2018/06/31 22:00" -after "2018/01/01 12:00"` will erase all bookmarks created between 01 Jan 2018, 12:00 and 31 June 2018, 22:00.
