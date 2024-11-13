package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"os/user"
	"runtime"
	"path/filepath"
	"strings"
	"time"

	toml "github.com/BurntSushi/toml"
	fsnotify "github.com/fsnotify/fsnotify"
)

type TargetFolder struct {
	TargetFolder string   `toml:"target_folder"`
	FileTypes    []string `toml:"file_types"`
}

type Settings struct {
	SourcePath       string         `toml:"source_path"`
	DaysAgo          int            `toml:"days_ago"`
	ExecutionMode    string         `toml:"execution_mode"`
	ExecutionInterval int            `toml:"execution_interval"`
	TargetFolders    []TargetFolder `toml:"target_folders"`
}

type Config struct {
	Settings []Settings `toml:"settings"`
}

// 创建默认配置文件
func createDefaultConfig(configFilePath string) {
	defaultConfig := `
[[settings]]
source_path = ""
days_ago = 3
execution_mode = "once"
execution_interval = 60

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

[[settings.target_folders]]
target_folder = "D:/Backup/Pictures"
file_types = ["jpg", "png", "gif"]

[[settings.target_folders]]
target_folder = "D:/Backup/Music"
file_types = ["mp3", "wav"]
`

	file, err := os.Create(configFilePath)
	if err != nil {
		log.Fatalf("Failed to create default config file: %v", err)
	}
	defer file.Close()

	_, err = file.WriteString(defaultConfig)
	if err != nil {
		log.Fatalf("Failed to write default config file: %v", err)
	}
	fmt.Println("Default configuration file created.")
}

// 加载配置信息
func loadConfig(configFilePath string) (Config, error) {
	var config Config
	if _, err := os.Stat(configFilePath); os.IsNotExist(err) {
		createDefaultConfig(configFilePath)
	}

	_, err := toml.DecodeFile(configFilePath, &config)
	if err != nil {
		return Config{}, fmt.Errorf("failed to decode config file: %v", err)
	}

	return config, nil
}

func getDesktopDirectory() (string, error) {
	// 获取当前用户信息
	currentUser, err := user.Current()
	if err != nil {
		return "", err
	}

	// 根据操作系统获取桌面目录
	switch runtime.GOOS {
	case "windows":
		// 对于 Windows，使用环境变量 %USERPROFILE%\\Desktop
		desktop := os.Getenv("USERPROFILE") + "\\Desktop"
		return desktop, nil
	case "darwin": // macOS
		// 对于 macOS，使用 ~/Desktop
		return currentUser.HomeDir + "/Desktop", nil
	default:
		// 对于其他操作系统，假设是 Linux
		return currentUser.HomeDir + "/Desktop", nil
	}
}

// 获取指定天数前的日期
func getDaysAgo(days int) time.Time {
	return time.Now().AddDate(0, 0, -days)
}

// 移动文件到目标文件夹
func moveFilesToFolders(sourcePath string, daysAgo int, targetFolders []TargetFolder) {
	cutoffTime := getDaysAgo(daysAgo)

	// 检查源目录是否存在
	if _, err := os.Stat(sourcePath); os.IsNotExist(err) {
		log.Fatalf("Source directory does not exist: %s", sourcePath)
	}

	files, err := os.ReadDir(sourcePath)
	if err != nil {
		log.Printf("Failed to read directory: %v", err)
		return
	}

	for _, file := range files {
		fileName := file.Name()
		filePath := filepath.Join(sourcePath, fileName)

		if file.IsDir() {
			continue
		}

		fileInfo, err := file.Info()
		if err != nil {
			log.Printf("Failed to get file info: %v", err)
			continue
		}

		lastAccessTime := fileInfo.ModTime()
		if lastAccessTime.Before(cutoffTime) {
			for _, folder := range targetFolders {
				if containsFileType(fileName, folder.FileTypes) {
					targetFolder := folder.TargetFolder
					if _, err := os.Stat(targetFolder); os.IsNotExist(err) {
						os.MkdirAll(targetFolder, os.ModePerm)
					}
					err := os.Rename(filePath, filepath.Join(targetFolder, fileName))
					if err != nil {
						log.Printf("Failed to move file: %v", err)
					}
					break
				}
			}
		}
	}
}

// 检查文件类型是否包含在列表中
func containsFileType(fileName string, fileTypes []string) bool {
	ext := strings.ToLower(filepath.Ext(fileName))
	for _, ft := range fileTypes {
		if ext == "."+ft {
			return true
		}
	}
	return false
}

// 文件监控事件处理器
type FileEventHandler struct {
	sourcePath     string
	daysAgo        int
	targetFolders  []TargetFolder
}

func (e *FileEventHandler) OnWrite(event fsnotify.Event) {
	moveFilesToFolders(e.sourcePath, e.daysAgo, e.targetFolders)
}

// 启动文件监控
func startMonitoring(sourcePath string, daysAgo int, targetFolders []TargetFolder) {
	watcher, err := fsnotify.NewWatcher()
	if err != nil {
		log.Fatalf("Failed to create watcher: %v", err)
	}
	defer watcher.Close()

	done := make(chan bool)
	go func() {
		for {
			select {
			case event, ok := <-watcher.Events:
				if !ok {
					return
				}
				e := &FileEventHandler{
					sourcePath:     sourcePath,
					daysAgo:        daysAgo,
					targetFolders:  targetFolders,
				}
				e.OnWrite(event)
			case err, ok := <-watcher.Errors:
				if !ok {
					return
				}
				log.Println("error:", err)
			}
		}
	}()

	err = watcher.Add(sourcePath)
	if err != nil {
		log.Fatalf("Failed to watch directory: %v", err)
	}
	<-done
}

// 周期性执行
func executePeriodically(sourcePath string, daysAgo int, targetFolders []TargetFolder, interval int) {
	ticker := time.NewTicker(time.Duration(interval) * time.Second)
	defer ticker.Stop()

	for range ticker.C {
		moveFilesToFolders(sourcePath, daysAgo, targetFolders)
	}
}

func main() {
	configFilePath := flag.String("config", "config.toml", "Path to the configuration file")
	flag.Parse()

	config, err := loadConfig(*configFilePath)
	if err != nil {
		log.Fatalf("Failed to load config: %v", err)
	}

	fmt.Println("Starting file organization...")

	// 处理每个 settings
	for _, setting := range config.Settings {
		f, _ := filepath.Match(setting.SourcePath, "")
		if f {
			path, err := getDesktopDirectory()
			if err != nil {
				log.Fatalf("Failed to get desktop directory: %v", err)
			}
			setting.SourcePath = path
		}
		// 检查源目录是否存在
		if _, err := os.Stat(setting.SourcePath); os.IsNotExist(err) {
			log.Fatalf("Source directory does not exist: %s", setting.SourcePath)
		}

		// 根据 execution_mode 进行不同的处理
		switch setting.ExecutionMode {
		case "once":
			moveFilesToFolders(setting.SourcePath, setting.DaysAgo, setting.TargetFolders)
		case "monitor":
			startMonitoring(setting.SourcePath, setting.DaysAgo, setting.TargetFolders)
		case "timer":
			executePeriodically(setting.SourcePath, setting.DaysAgo, setting.TargetFolders, setting.ExecutionInterval)
		default:
			fmt.Println("Invalid execution mode. Please set 'execution_mode' to 'once', 'monitor', or 'timer'.")
		}
	}
}
