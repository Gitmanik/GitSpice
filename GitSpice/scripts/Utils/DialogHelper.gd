extends Node

var last_directory = OS.get_executable_path().get_base_dir()

var file_dialog: NativeFileDialog = null

@onready
var uic = get_node("/root/main/ElementContainer")

func open_file_dialog():
	file_dialog = NativeFileDialog.new()
	file_dialog.title = "Open Circuit"
	file_dialog.file_mode = NativeFileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = NativeFileDialog.ACCESS_FILESYSTEM
	file_dialog.root_subfolder = last_directory
	file_dialog.connect("file_selected", on_file_open_selected)
	file_dialog.connect("canceled", on_canceled)

	add_child(file_dialog)
	file_dialog.show()

func save_file_dialog():
	file_dialog = NativeFileDialog.new()
	file_dialog.title = "Save Circuit"
	file_dialog.file_mode = NativeFileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = NativeFileDialog.ACCESS_FILESYSTEM
	file_dialog.root_subfolder = last_directory
	file_dialog.connect("file_selected", on_file_save_selected)
	file_dialog.connect("canceled", on_canceled)

	add_child(file_dialog)
	file_dialog.show()

func on_file_open_selected(test):
	last_directory = test.get_base_dir()
	uic.load_circuit(test)
	file_dialog.queue_free()

func on_file_save_selected(test):
	last_directory = test.get_base_dir()
	uic.save_circuit(test)
	file_dialog.queue_free()

func on_canceled():
	file_dialog.queue_free()
