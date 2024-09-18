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
import sys
import time
import shutil
import configparser
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

[settings_1]
source_path = ""
days_ago = 3
execution_mode = once

[target_folder_1]
target_folder = D:/Backup/Documents
file_types = .txt,.xlsx,.docx

[target_folder_2]
target_folder = D:/Backup/Videos
file_types = .mp4,.avi

[settings_2]
source_path = ""
days_ago = 5
execution_mode = monitor
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

    config = configparser.ConfigParser()
    config.read(config_file_path)

    settings_list = []
    for section in config.sections():
        if section.startswith('settings'):
            settings = {
                'source_path': config[section].get('source_path'),
                'days_ago': int(config[section].get('days_ago', 3)),
                'execution_mode': config[section].get('execution_mode', 'once')
            }
            settings_list.append(settings)

    target_folders = {}
    for section in config.sections():
        if section.startswith('target_folder'):
            target_folder = config[section].get('target_folder', '')
            file_types = config[section].get('file_types', '').split(',')
            target_folders[section] = {
                'target_folder': target_folder,
                'file_types': file_types
            }
    print(settings_list)

    return settings_list, target_folders


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
    cutoff_time = get_days_ago(days_ago)
    # 遍历源文件夹中的所有文件
    for file_name in os.listdir(source_path):
        # 检查文件类型
        for section, info in target_folders.items():
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


if __name__ == '__main__':
    config_file_path = "config.ini"
    settings_list, target_folders = load_config(config_file_path)

    print("Starting file organization...")

    # 确保所有目标文件夹都存在
    for section, info in target_folders.items():
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
            start_monitoring(source_path, days_ago, target_folders)
        elif execution_mode == 'timer':
            execute_periodically(source_path, days_ago,
                                 target_folders, execution_interval)
        else:
            print(
                "Invalid execution mode. Please set 'execution_mode' to 'once', 'monitor', or 'timer'.")
