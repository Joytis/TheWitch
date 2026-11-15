# Converts Spine editor exports into the spine-godot resource formats the game can load.
#
# The game's spine-godot GDExtension (addons/spine, copied from gamedata/ by tools/setup.ps1) is a
# runtime build: it registers the .spskel/.spatlas loaders but not the .skel/.atlas editor importers,
# so a raw Spine export never becomes a resource. This script does that conversion by hand. The saved
# files are self-contained (skeleton bytes / atlas text embedded); atlas pages stay plain imported pngs,
# looked up next to the .spatlas at load time.
#
# Run from tools/export-spine.ps1, or directly (after a --import so the atlas pngs are imported):
#   godot --headless --path <project> -s <repo>/tools/spine/convert-spine.gd -- res://a/b.skel res://a/b.atlas
# Each input is saved beside itself: b.skel -> b.spskel, b.atlas -> b.spatlas. Exit code 1 on any failure.
extends SceneTree


func _init() -> void:
	var failed := false
	var inputs := OS.get_cmdline_user_args()
	if inputs.is_empty():
		push_error("convert-spine: no inputs (pass res:// paths after --)")
		failed = true
	for path in inputs:
		if not _convert(path):
			failed = true
	quit(1 if failed else 0)


func _convert(path: String) -> bool:
	var res: Resource
	var out := path.get_basename()
	var err: int
	if path.ends_with(".skel") or path.ends_with(".json"):
		res = ClassDB.instantiate("SpineSkeletonFileResource")
		if res == null:
			push_error("convert-spine: SpineSkeletonFileResource missing - is addons/spine in the project?")
			return false
		err = res.load_from_file(path)
		out += ".spskel"
	elif path.ends_with(".atlas"):
		res = ClassDB.instantiate("SpineAtlasResource")
		if res == null:
			push_error("convert-spine: SpineAtlasResource missing - is addons/spine in the project?")
			return false
		err = res.load_from_atlas_file(path)
		out += ".spatlas"
	else:
		push_error("convert-spine: unsupported input %s" % path)
		return false

	if err != OK:
		push_error("convert-spine: failed to load %s (error %d)" % [path, err])
		return false
	err = ResourceSaver.save(res, out)
	if err != OK:
		push_error("convert-spine: failed to save %s (error %d)" % [out, err])
		return false
	print("convert-spine: %s -> %s" % [path, out])
	return true
