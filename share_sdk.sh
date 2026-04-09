# !/bin/bash


UNITY_PROJ=BrainPunk


# 将研发本地的 SDK 软连接抽取出来做成 zip， 可以方便的分享给美术或者产品
# 1. 扫描 $UNITY_PROJ/Packages 路径下，并找到所有的软连接
# 2. 获取这些软连接的原身，并拷贝到 $UNITY_PROJ/../temp 目录内
# 3. 将 temp 打包为 zip 文件， 并重命名为 'shared_sdk_{yymmdd}.zip' 其中 yymmdd 代表当前时间的年月日，例如：250702
# 4. 在压缩的时候，请直接压缩 temp 内的所有目录和文件，也就是说， 我希望解压缩后，一级目录不是 temp，而应该直接就是各个包的文件夹
# 5. 最后删除 temp 目录， 并打开当前的路径

# 1. 扫描 $UNITY_PROJ/Packages 路径下，并找到所有的软连接
echo "正在扫描 $UNITY_PROJ/Packages 目录下的软连接..."
SYMLINKS=$(find "$UNITY_PROJ/Packages" -type l 2>/dev/null)

if [ -z "$SYMLINKS" ]; then
    echo "未找到任何软连接，退出脚本"
    exit 1
fi

echo "找到以下软连接："
echo "$SYMLINKS"

# 2. 获取这些软连接的原身，并拷贝到 $UNITY_PROJ/../temp 目录内
TEMP_DIR="$UNITY_PROJ/../temp"
echo "创建临时目录: $TEMP_DIR"
rm -rf "$TEMP_DIR"
mkdir -p "$TEMP_DIR"

echo "正在拷贝软连接的原身到临时目录..."
while IFS= read -r symlink; do
    if [ -n "$symlink" ]; then
        # 获取软连接的原身路径
        real_path=$(readlink "$symlink")
        # 获取包名（软连接的文件夹名）
        package_name=$(basename "$symlink")
        
        echo "拷贝 $package_name -> $real_path"
        
        # 如果原身是相对路径，需要转换为绝对路径
        if [[ "$real_path" != /* ]]; then
            real_path="$(dirname "$symlink")/$real_path"
        fi
        
        # 拷贝到临时目录
        if [ -d "$real_path" ]; then
            cp -r "$real_path" "$TEMP_DIR/$package_name"
        else
            echo "警告: $real_path 不存在或不是目录"
        fi
    fi
done <<< "$SYMLINKS"

# 3. 将 temp 打包为 zip 文件，并重命名为 'shared_sdk_{yymmdd}.zip'
# 获取当前日期，格式为 yymmdd
DATE=$(date +%y%m%d)
ZIP_NAME="shared_sdk_${DATE}.zip"

echo "正在创建压缩包: $ZIP_NAME"

# 4. 直接压缩 temp 内的所有目录和文件，解压后一级目录不是 temp
cd "$TEMP_DIR"
zip -r "../$ZIP_NAME" . > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "压缩包创建成功: $(pwd)/../$ZIP_NAME"
else
    echo "压缩包创建失败"
    exit 1
fi

# 回到原目录
cd - > /dev/null

# 5. 最后删除 temp 目录，并打开当前的路径
echo "清理临时目录..."
rm -rf "$TEMP_DIR"

echo "脚本执行完成！"
echo "生成的压缩包: $(realpath "$UNITY_PROJ/../$ZIP_NAME")"

# 打开当前路径（在 macOS 上使用 open 命令）
if command -v open > /dev/null 2>&1; then
    open "$(dirname "$(realpath "$UNITY_PROJ/../$ZIP_NAME")")"
elif command -v xdg-open > /dev/null 2>&1; then
    xdg-open "$(dirname "$(realpath "$UNITY_PROJ/../$ZIP_NAME")")"
else
    echo "请手动打开目录: $(dirname "$(realpath "$UNITY_PROJ/../$ZIP_NAME")")"
fi

