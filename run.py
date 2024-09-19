# -*- coding: utf-8 -*-
"""
@FilePath: /run.py
@Description: TODO
@Author: zhangt tao993859833@live.cn
@Date: 2024-09-19 11:00:36
@LastEditors: zhangt tao993859833@live.cn
@LastEditTime: 2024-09-19 13:44:57
@世界上最遥远的距离不是生与死，而是你亲手制造的BUG就在你眼前，你却怎么都找不到她
@Copyright (c) 2024 by zhangt email: tao993859833@live.cn, All Rights Reserved
"""
import os
import sys
import signal
import subprocess


class ScriptRunner:
    def __init__(self, script_path):
        self.script_path = script_path
        self.process = None
        self.pid_file = 'script.pid'

    def start_script(self):
        try:
            if os.name == 'nt':
                startupinfo = subprocess.STARTUPINFO()
                startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
                startupinfo.wShowWindow = subprocess.SW_HIDE
                self.process = subprocess.Popen(["python", self.script_path], startupinfo=startupinfo,
                                                creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
            else:
                self.process = subprocess.Popen(["python", self.script_path], stdout=subprocess.DEVNULL,
                                                stderr=subprocess.DEVNULL, start_new_session=True)

            with open(self.pid_file, 'w') as f:
                f.write(str(self.process.pid))

            print(f"Started script: {self.script_path}")

        except Exception as e:
            print(f"Error starting script: {e}")

    def stop_script(self):
        if os.path.exists(self.pid_file):
            with open(self.pid_file, 'r') as f:
                pid = int(f.read().strip())
            try:
                if os.name == 'nt':
                    os.kill(pid, signal.CTRL_BREAK_EVENT)
                else:
                    os.killpg(os.getpgid(pid), signal.SIGTERM)
                os.remove(self.pid_file)
                print(f"Stopped script: {self.script_path}")
            except Exception as e:
                print(f"Error stopping script: {e}")
        else:
            print("No script is currently running.")


if __name__ == "__main__":
    script_path = "organize.py"
    runner = ScriptRunner(script_path)

    if len(sys.argv) == 2:
        if 'start' == sys.argv[1]:
            runner.start_script()
        elif 'stop' == sys.argv[1]:
            runner.stop_script()
        else:
            print(f"Unknown command {sys.argv[1]}.")
            sys.exit(2)
        sys.exit(0)
    else:
        print("usage: %s start|stop" % sys.argv[0])
        sys.exit(2)
