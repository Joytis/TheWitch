# Converts one Spine editor export (.skel + .atlas, sitting next to the atlas page pngs) into the
# self-contained resources the game's spine-godot runtime loads: .spskel + .spatlas.
#
# Run by tools/export-spine.ps1 inside a throwaway Godot project that carries the game's Spine
# GDExtension; not meant to be run by hand. The game's extension is the runtime-only build: it
# registers the Spine resource classes and the .spskel/.spatlas savers, but not the editor import
# plugins for .skel/.atlas, so the conversion goes through the resource API instead.
#
#   godot --headless --path <stage project> --script convert_spine.gd -- <res:// base path, no extension>
#
# Prints one "SPINE_INFO {json}" line (animation names, setup-pose bounds) for the caller.
extends SceneTree


func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1:
		_fail("usage: convert_spine.gd -- <res:// base path without extension>")
		return
	for cls in ["SpineAtlasResource", "SpineSkeletonFileResource", "SpineSkeletonDataResource", "SpineSprite"]:
		if not ClassDB.class_exists(cls):
			_fail("%s is not registered: the Spine GDExtension did not load" % cls)
			return
	var base: String = args[0]

	var atlas: Resource = ClassDB.instantiate("SpineAtlasResource")
	var err: int = atlas.load_from_atlas_file(base + ".atlas")
	if err != OK:
		_fail("load_from_atlas_file(%s.atlas) failed: %s" % [base, error_string(err)])
		return
	err = ResourceSaver.save(atlas, base + ".spatlas")
	if err != OK:
		_fail("saving %s.spatlas failed: %s" % [base, error_string(err)])
		return

	var skel: Resource = ClassDB.instantiate("SpineSkeletonFileResource")
	err = skel.load_from_file(base + ".skel")
	if err != OK:
		_fail("load_from_file(%s.skel) failed: %s" % [base, error_string(err)])
		return
	err = ResourceSaver.save(skel, base + ".spskel")
	if err != OK:
		_fail("saving %s.spskel failed: %s" % [base, error_string(err)])
		return

	# Round-trip check: the saved files alone must build a usable skeleton.
	var data: Resource = ClassDB.instantiate("SpineSkeletonDataResource")
	data.atlas_res = ResourceLoader.load(base + ".spatlas", "", ResourceLoader.CACHE_MODE_IGNORE)
	data.skeleton_file_res = ResourceLoader.load(base + ".spskel", "", ResourceLoader.CACHE_MODE_IGNORE)
	if not data.is_skeleton_data_loaded():
		_fail("the saved .spskel/.spatlas did not load back into a skeleton")
		return

	var names: Array[String] = []
	for anim in data.get_animations():
		names.append(anim.get_name())

	var sprite: Node2D = ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res = data
	root.add_child(sprite)
	var bounds: Rect2 = sprite.get_skeleton().get_bounds()
	sprite.queue_free()

	print("SPINE_INFO ", JSON.stringify({
		"animations": names,
		"bounds": [bounds.position.x, bounds.position.y, bounds.size.x, bounds.size.y],
	}))
	quit(0)


func _fail(message: String) -> void:
	printerr("convert_spine: " + message)
	quit(1)
