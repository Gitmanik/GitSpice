extends Node

var file_dialog: NativeFileDialog = null

@onready
var uic = get_node("/root/main/ElementContainer")

func open_file_dialog(base_directory):
	file_dialog = NativeFileDialog.new()
	file_dialog.title = "Open Circuit"
	file_dialog.file_mode = NativeFileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = NativeFileDialog.ACCESS_FILESYSTEM
	file_dialog.root_subfolder = base_directory
	file_dialog.connect("file_selected", on_file_open_selected)
	file_dialog.connect("canceled", on_canceled)

	add_child(file_dialog)
	file_dialog.show()

func save_file_dialog(base_directory):
	file_dialog = NativeFileDialog.new()
	file_dialog.title = "Save Circuit"
	file_dialog.file_mode = NativeFileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = NativeFileDialog.ACCESS_FILESYSTEM
	file_dialog.root_subfolder = base_directory
	file_dialog.connect("file_selected", on_file_save_selected)
	file_dialog.connect("canceled", on_canceled)

	add_child(file_dialog)
	file_dialog.show()

func on_file_open_selected(file_path):
	uic.load_circuit(file_path)
	file_dialog.queue_free()

func on_file_save_selected(file_path):
	uic.save_circuit(file_path)
	file_dialog.queue_free()

func on_canceled():
	file_dialog.queue_free()
