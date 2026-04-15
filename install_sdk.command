#!/bin/bash

export CUR_DIR="$( cd "$( dirname "$0" )" && pwd )"
export GURU_HOME=${HOME}/.guru
export CLI_HOME=${GURU_HOME}/guru_config
export CLI=$GURU_HOME/guru_config/buildtools/unitysdk_cli.py
export GURU_CONFIG_URL=git@github.com:castbox/guru_config.git
export USING_GF=1

# 这里定义 中台和项目组引入库冲突时：
#   1.如果使用中台版本库下面不要配置 
#   2.如果使用项目组自己引入的库则需要配置移除中台库的列表，这里多条配置使用换行配置
REMOVE_PACKAGES=(
    
)


# 查找Unity项目路径
function find_unity_path() {
    local find_paths=(
        "$CUR_DIR"
        "$(dirname "$CUR_DIR")"
    )

    local unity_root=""
    for find_path in "${find_paths[@]}"; do
        echo "Checking path: $find_path" >&2
        unity_root=$(find_unity_root "$find_path")
        if [ -n "$unity_root" ]; then
            break
        fi
    done

    if [ -n "$unity_root" ]; then
        echo "$unity_root"
        return 0
    else
        echo "Error: Cannot find Unity project root directory" >&2
        echo "Please ensure you are running this script from within a Unity project structure" >&2
        return 1
    fi
}

# 查找Unity项目根目录
function find_unity_root ()
{
    parent_dir="${1:-$CUR_DIR}"
    local unity_root=""
    
    echo "Searching for Unity project root in: $parent_dir" >&2
    
    # 使用find命令查找包含Assets和ProjectSettings的目录
    # 排除.git、Library等目录，限制查找深度为5层
    for dir in $(find "$parent_dir" -maxdepth 5 -type d \( -name ".git" -o -name "Library" -o -name "Temp" -o -name "obj" \) -prune -o -print 2>/dev/null); do
        if [ -d "${dir}/Assets" ] && [ -d "${dir}/ProjectSettings" ]; then
            unity_root="$dir"
            echo "Found Unity project at: $unity_root" >&2
            break
        fi
    done
    if [ -n "$unity_root" ]; then
        echo "$unity_root"
        return 0
    else
        return 1
    fi
}

# 查找项目sdk version版本
function read_sdk_version()
{
    local unity_root=${1}
    local guru_sdk_path=$(find ${unity_root}/Assets -maxdepth 3 -type f -name "guru_sdk.yaml")
    local sdk_version=$(grep "guru_sdk:" ${guru_sdk_path} | awk '{print $2}')
    if [ -n "$sdk_version" ]; then
        echo "$sdk_version"
        return 0
    else
        return 1
    fi
}

# 检查和创建本地cli路径是否存在
function check_or_update_cli()
{
    # check or create dir
    if [ ! -d "$CLI_HOME" ]; then
        mkdir -p "$CLI_HOME"
        cd "$CLI_HOME"
        git clone --depth 1 $GURU_CONFIG_URL
        echo "Directory created: $CLI_HOME"
    else
        cd "$CLI_HOME"
        git pull --rebase
        echo "Directory already exists: $CLI_HOME"
    fi    
    echo
}

# 安装sdk
function install_sdk()
{
    echo
    echo "--- unity_prject: ${UNITY_PROJ} ---"
    echo
    echo "--- install [$SDK_VERSION] sdk-v2 from cli ---"
    echo

    #修复导包报错问题
    find ${UNITY_PROJ}/Packages -type l -exec rm {} \;

    cd $UNITY_PROJ

    # install sdk by cli
    python3 $CLI get $SDK_VERSION
}

# 移除中台冲突包
function remove_packages()
{
    echo
    echo "开移除冲突的包"
    # Loop through each path in the array
    for package in "${REMOVE_PACKAGES[@]}"; do
        REMOVE_PACKAGE=${UNITY_PROJ}/Packages/${package}
        echo "${REMOVE_PACKAGE}"
        if [ -d "${REMOVE_PACKAGE}" ]; then
            rm -rf ${REMOVE_PACKAGE}
        fi
    done
}


function init_gf()
{
    # 检查是否使用 GF 模块
    if [ "$USING_GF" == "0" ]; then
        echo "项目不使用 GF 模块，跳过 GF 初始化"
        return 0
    fi
    
    echo
    echo "--- 初始化 GF 模块 ---"
    
    # 定义 GF 相关变量
    local GF_REPO=git@github.com:castbox/com.guru.unity.gf.git
    local GF_PATH="${UNITY_PROJ}/Assets/GF"
    
    # 进入项目根目录
    cd "$UNITY_PROJ"
    
    # 初始化 git submodule
    git submodule init
    
    # 检查 GF 目录是否存在
    if [ ! -d "$GF_PATH" ]; then
        echo "GF 目录不存在，添加 submodule..."
        git submodule add $GF_REPO ./Assets/GF
    else
        echo "GF 目录已存在，仅更新 submodule..."
    fi
    
    # 更新 submodule
    git submodule update
    
    echo "GF 模块初始化完成"
    echo
}



function main()
{
    UNITY_PROJ=$(find_unity_path)
    SDK_VERSION=$(read_sdk_version $UNITY_PROJ)

    check_or_update_cli
    install_sdk
    remove_packages
    init_gf

    echo
    echo "--- install completed ---"
    echo
}

main
exit 0