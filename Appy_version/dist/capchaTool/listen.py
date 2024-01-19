import time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
import json
import os

class MyHandler(FileSystemEventHandler):
    check = True
    data = ""
    
    def on_modified(self, event):
        if event.src_path.endswith(".json"):
            try:
                with open(event.src_path, "r") as f:
                    data = json.load(f)
                    if data[-1]["user"] == "A":
                        self.data = data[-1]['notify']
                        self.check = False
            except (FileNotFoundError, json.JSONDecodeError):
                pass
# Hàm này dùng để đọc dữ lieu ra
def track():
    observer = Observer()
    event_handler = MyHandler()
    observer.schedule(event_handler, path="H:\\3i-Intern\\NewProjectWinForm_SmartWork\\AppNet2\\AppNet\\Appnet_2\\bin\\Debug", recursive=False)
    observer.start()

    while event_handler.check:
        time.sleep(1)

    print("end")
    observer.stop()
    observer.join()

    return event_handler.data
