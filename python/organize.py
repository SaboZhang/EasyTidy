# coding=utf-8
'''
 @Author       : Rick
 @Date         : 2024-09-18 20:01:29
 @LastEditors  : Rick tao993859833@live.cn
 @LastEditTime : 2024-09-19 00:55:16
 @Description  : TODO
 @Copyright (c) 2024 by Rick && rick@luckyits.com All Rights Reserved.
'''
import os
import datetime
from pathlib import Path
import queue
import sys
import threading
import time
import shutil
import toml
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
try:
    import win32com.client
except ImportError:
    win32com = None


# 获取当前日期的几天前
def get_days_ago(days):
    return datetime.datetime.now() - datetime.timedelta(days=days)


def create_default_config(config_file_path):
    """
    创建默认配置文件。
    :param config_file_path: 配置文件路径
    """

    default_config = """

[[settings]]
source_path = ""
days_ago = 3
execution_mode = "once"
execution_interval = 60
file_conflict = "skip"

[[settings.target_folders]]
target_folder = "D:/Backup/Documents"
file_types = ["txt", "xlsx", "docx"]

[[settings.target_folders]]
target_folder = "D:/Backup/Videos"
file_types = ["mp4", "avi"]

[[settings]]
source_path = ""
days_ago = 5
execution_mode = "monitor"
execution_interval = 120
file_conflict = "skip"

[[settings.target_folders]]
target_folder = "D:/Backup/Pictures"
file_types = ["jpg", "png", "gif"]

[[settings.target_folders]]
target_folder = "D:/Backup/Music"
file_types = ["mp3", "wav"]
"""

    with open(config_file_path, 'w') as config_file:
        config_file.write(default_config)


def load_config(config_file_path):
    """
    从 INI 文件中加载配置信息。
    :param config_file_path: 配置文件路径
    :return: 配置信息字典列表
    """

    if not os.path.exists(config_file_path):
        create_default_config(config_file_path)
        print(f"Default configuration file created at {
              config_file_path}. Please configure [target_folder_1].")

    with open(config_file_path, 'r') as f:
        config = toml.load(f)

    settings_list = []

    for settings in config['settings']:
        source_path = settings['source_path']
        if not os.path.exists(source_path):
            source_path = get_desktop_path()
        days_ago = settings['days_ago']
        execution_mode = settings['execution_mode']
        execution_interval = settings.get('execution_interval', 60)
        file_conflict = settings.get('file_conflict', 'rename')
        target_folders_section = []

        for folder in settings['target_folders']:
            target_folder = folder['target_folder']
            file_types = folder['file_types']
            target_folders_section.append({
                'target_folder': target_folder,
                'file_types': file_types
            })

        settings_list.append({
            'source_path': source_path,
            'days_ago': days_ago,
            'execution_mode': execution_mode,
            'execution_interval': execution_interval,
            'file_conflict': file_conflict,
            'target_folders': target_folders_section
        })

    return settings_list, target_folders_section


def get_desktop_path():
    if sys.platform.startswith('win32') and win32com is not None:
        # Windows 系统且有 win32com
        shell = win32com.client.Dispatch("WScript.Shell")
        desktop_path = shell.SpecialFolders("Desktop")
    else:
        # 其他系统（Linux, macOS）
        user_home = str(Path.home())
        desktop_path = os.path.join(user_home, 'Desktop')

    return desktop_path


def move_files_to_folders(source_path, days_ago, target_folders, file_conflict='rename'):
    """
    将指定文件夹中的指定类型的文件移动到指定的目标文件夹。
    :param source_path: 源文件夹路径
    :param days_ago: 天数
    :param target_folders: 目标文件夹及其对应的文件类型
    :param file_conflict: 文件冲突处理方式 ('rename', 'skip', 'overwrite')
    """

    print("Moving files to folders...")
    # 计算几天前的时间
    cutoff_time = get_days_ago(days_ago)
    # 遍历源文件夹中的所有文件
    for file_name in os.listdir(source_path):
        # 检查文件类型
        for info in target_folders:
            if any(file_name.endswith(ft) for ft in info['file_types']):
                file_path = os.path.join(source_path, file_name)
                # 检查文件的最后访问时间
                last_access_time = datetime.datetime.fromtimestamp(
                    os.path.getatime(file_path))
                if last_access_time < cutoff_time:
                    # 移动文件到目标文件夹
                    target_folder = info['target_folder']
                    if not os.path.exists(target_folder):
                        os.makedirs(target_folder)

                    target_file_path = os.path.join(target_folder, file_name)
                    if os.path.exists(target_file_path):
                        if file_conflict == 'rename':
                            base_name, ext = os.path.splitext(file_name)
                            counter = 1
                            while os.path.exists(os.path.join(target_folder, f"{base_name}_{counter}{ext}")):
                                counter += 1
                            new_file_name = f"{base_name}_{counter}{ext}"
                            shutil.move(file_path, os.path.join(target_folder, new_file_name))
                        elif file_conflict == 'skip':
                            continue
                        elif file_conflict == 'overwrite':
                            file = newer_files(file_path, target_file_path)
                            if file == file_path:
                                shutil.move(file_path, target_file_path)
                            else:
                                continue
                    else:
                        shutil.move(file_path, target_file_path)
                    break  # 移动后停止进一步检查


def newer_files(source_path, target_file_path):
    """
    判断哪个文件更新。

    :param file1: 第一个文件的路径
    :param file2: 第二个文件的路径
    :return: 更新的文件路径，如果两个文件时间相同则返回 None
    """
    # 获取文件的最后修改时间
    mtime1 = os.path.getmtime(source_path)
    mtime2 = os.path.getmtime(target_file_path)

    # 比较两个文件的最后修改时间
    if mtime1 > mtime2:
        return source_path
    elif mtime1 < mtime2:
        return target_file_path
    else:
        return None


class FileEventHandler(FileSystemEventHandler):

    def __init__(self, source_path, days_ago, target_folders, file_conflict='rename'):
        self.source_path = source_path
        self.days_ago = days_ago
        self.target_folders = target_folders
        self.file_conflict = file_conflict

    def on_modified(self, event):
        if event.is_directory:
            return
        move_files_to_folders(
            self.source_path, self.days_ago, self.target_folders, self.file_conflict)


def start_monitoring(source_path, days_ago, target_folders, file_conflict='rename'):
    print("Monitoring started.")
    observer = Observer()
    event_handler = FileEventHandler(source_path, days_ago, target_folders, file_conflict)
    observer.schedule(event_handler, path=source_path, recursive=True)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()


def execute_periodically(source_path, days_ago, target_folders, interval, file_conflict='rename'):
    print("Periodic execution started.")
    while True:
        move_files_to_folders(source_path, days_ago, target_folders, file_conflict)
        time.sleep(interval)


# 定义一个工作线程来处理任务
def worker(task_queue):
    while True:
        task = task_queue.get()
        if task is None:  # 用于停止线程
            break
        execution_mode, settings = task
        source_path = settings['source_path']
        days_ago = settings['days_ago']
        target_folders = settings['target_folders']
        execution_interval = settings.get('execution_interval', 60)
        file_conflict = settings.get('file_conflict', 'rename')

        if execution_mode == 'monitor':
            start_monitoring(source_path, days_ago, target_folders, file_conflict)
        elif execution_mode == 'timer':
            execute_periodically(source_path, days_ago, target_folders, execution_interval, file_conflict)

        task_queue.task_done()


if __name__ == '__main__':
    config_file_path = "config.toml"
    settings_list, target_folders = load_config(config_file_path)

    print("Starting file organization...")

    # 创建任务队列
    task_queue = queue.Queue()

    # 启动多个工作线程
    num_worker_threads = len(settings_list) + 1
    threads = []

    for i in range(num_worker_threads):
        t = threading.Thread(target=worker, args=(task_queue,))
        t.start()
        threads.append(t)

    # 确保所有目标文件夹都存在
    for info in target_folders:
        target_folder = info['target_folder']
        if not os.path.exists(target_folder):
            os.makedirs(target_folder)

    # 根据每个设置部分进行不同的处理
    for settings in settings_list:
        source_path = settings['source_path']
        days_ago = settings['days_ago']
        execution_mode = settings['execution_mode']
        execution_interval = settings.get('execution_interval', 60)
        file_conflict = settings.get('file_conflict', 'rename')

        if execution_mode == 'once':
            move_files_to_folders(source_path, days_ago, target_folders, file_conflict)
        elif execution_mode == 'monitor':
            task_queue.put(('monitor', settings))
        elif execution_mode == 'timer':
            task_queue.put(('timer', settings))
        else:
            print("Invalid execution mode. Please set 'execution_mode' to 'once', 'monitor', or 'timer'.")

    # 等待所有任务完成
    task_queue.join()

    # 停止所有工作线程
    for i in range(num_worker_threads):
        task_queue.put(None)
    for t in threads:
        t.join()
