# -*- coding: utf-8 -*-
"""
@FilePath: /temp.py
@Description: TODO
@Author: zhangt tao993859833@live.cn
@Date: 2024-09-19 16:55:07
@LastEditors: zhangt tao993859833@live.cn
@LastEditTime: 2024-09-19 16:55:10
@世界上最遥远的距离不是生与死，而是你亲手制造的BUG就在你眼前，你却怎么都找不到她
@Copyright (c) 2024 by zhangt email: tao993859833@live.cn, All Rights Reserved
"""
import toml
import os
import sys
import win32com.client
from pathlib import Path
import shutil
import datetime
import time
import threading
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
import queue


def load_config(config_file_path):
    with open(config_file_path, 'r') as f:
        config = toml.load(f)

    settings_list = []
    target_folders = {}

    for settings in config['settings']:
        source_path = settings['source_path']
        days_ago = settings['days_ago']
        execution_mode = settings['execution_mode']
        execution_interval = settings.get('execution_interval', 60)
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


def move_files_to_folders(source_path, days_ago, target_folders):
    """
    将指定文件夹中的指定类型的文件移动到指定的目标文件夹。
    :param source_path: 源文件夹路径
    :param days_ago: 天数
    :param target_folders: 目标文件夹及其对应的文件类型
    """

    print("Moving files to folders...")
    # 计算几天前的时间
    cutoff_time = datetime.datetime.now() - datetime.timedelta(days=days_ago)
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
                    shutil.move(file_path, os.path.join(
                        target_folder, file_name))
                    break  # 移动后停止进一步检查


class FileEventHandler(FileSystemEventHandler):

    def __init__(self, source_path, days_ago, target_folders):
        self.source_path = source_path
        self.days_ago = days_ago
        self.target_folders = target_folders

    def on_modified(self, event):
        if event.is_directory:
            return
        move_files_to_folders(
            self.source_path, self.days_ago, self.target_folders)


def start_monitoring(source_path, days_ago, target_folders):
    print("Monitoring started.")
    observer = Observer()
    event_handler = FileEventHandler(source_path, days_ago, target_folders)
    observer.schedule(event_handler, path=source_path, recursive=True)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()


def execute_periodically(source_path, days_ago, target_folders, interval):
    print("Periodic execution started.")
    while True:
        move_files_to_folders(source_path, days_ago, target_folders)
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

        if execution_mode == 'monitor':
            start_monitoring(source_path, days_ago, target_folders)
        elif execution_mode == 'timer':
            execute_periodically(source_path, days_ago, target_folders, execution_interval)

        task_queue.task_done()


if __name__ == '__main__':
    config_file_path = "config.toml"
    settings_list, target_folders = load_config(config_file_path)

    print("Starting file organization...")

    # 创建任务队列
    task_queue = queue.Queue()

    # 启动多个工作线程
    num_worker_threads = 5
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
        if not os.path.exists(source_path):
            source_path = get_desktop_path()
        days_ago = settings['days_ago']
        execution_mode = settings['execution_mode']
        execution_interval = settings.get('execution_interval', 60)

        if execution_mode == 'once':
            move_files_to_folders(source_path, days_ago, target_folders)
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