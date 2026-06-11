<template>
    <div>
        <!-- 卡片组件， shadow="never" 指定 card 卡片组件没有阴影 -->
        <el-card shadow="never">
            <el-form ref="formRef" :model="form" label-width="160px" :rules="rules">
                <el-form-item>
                    <h2 class="font-bold text-base mb-1">基础设置</h2>
                </el-form-item>
                <el-form-item label="博客名称" prop="name">
                    <el-input v-model="form.name" clearable />
                </el-form-item>
                <el-form-item label="作者名" prop="author">
                    <el-input v-model="form.author" clearable />
                </el-form-item>
                <el-form-item label="博客 LOGO" prop="logo">
                    <el-upload class="avatar-uploader" action="#" :on-change="handleLogoChange" :auto-upload="false"
                        :show-file-list="false">
                        <img v-if="form.logo" :src="form.logo" class="avatar" />
                        <el-icon v-else class="avatar-uploader-icon">
                            <Plus />
                        </el-icon>
                    </el-upload>
                </el-form-item>
                <el-form-item label="作者头像" prop="avatar">
                    <el-upload class="avatar-uploader" action="#" :on-change="handleAvatarChange" :auto-upload="false"
                        :show-file-list="false">
                        <img v-if="form.avatar" :src="form.avatar" class="avatar" />
                        <el-icon v-else class="avatar-uploader-icon">
                            <Plus />
                        </el-icon>
                    </el-upload>
                </el-form-item>
                <el-form-item label="介绍语" prop="introduction">
                    <el-input v-model="form.introduction" type="textarea" />
                </el-form-item>

                <!-- 分割线 -->
                <el-divider />

                <el-form-item>
                    <h2 class="font-bold text-base mb-1">第三方平台设置</h2>
                </el-form-item>
                <!-- 开启 Github 访问 -->
                <el-form-item label="开启 GihHub 访问">
                    <el-switch v-model="isGithubChecked" inline-prompt :active-icon="Check" :inactive-icon="Close"
                        @change="githubSwitchChange" />
                </el-form-item>
                <el-form-item label="GitHub 主页访问地址" v-if="isGithubChecked">
                    <el-input v-model="form.githubHomepage" clearable placeholder="请输入 GitHub 主页访问的 URL" />
                </el-form-item>

                <!-- 开启 Gitee 访问 -->
                <el-form-item label="开启 Gitee 访问">
                    <el-switch v-model="isGiteeChecked" inline-prompt :active-icon="Check" :inactive-icon="Close"
                        @change="giteeSwitchChange" />
                </el-form-item>
                <el-form-item label="Gitee 主页访问地址" v-if="isGiteeChecked">
                    <el-input v-model="form.giteeHomepage" clearable placeholder="请输入 Gitee 主页访问的 URL" />
                </el-form-item>

                <!-- 开启知乎访问 -->
                <el-form-item label="开启知乎访问">
                    <el-switch v-model="isZhihuChecked" inline-prompt :active-icon="Check" :inactive-icon="Close"
                        @change="zhihuSwitchChange" />
                </el-form-item>
                <el-form-item label="知乎主页访问地址" v-if="isZhihuChecked">
                    <el-input v-model="form.zhihuHomepage" clearable placeholder="请输入知乎主页访问的 URL" />
                </el-form-item>

                <!-- 开启 CSDN 访问 -->
                <el-form-item label="开启 CSDN 访问">
                    <el-switch v-model="isCSDNChecked" inline-prompt :active-icon="Check" :inactive-icon="Close"
                        @change="csdnSwitchChange" />
                </el-form-item>
                <el-form-item label="CSDN 主页访问地址" v-if="isCSDNChecked">
                    <el-input v-model="form.csdnHomepage" clearable placeholder="请输入 CSDN 主页访问的 URL" />
                </el-form-item>

                <!-- 分割线 -->
                <el-divider />

                <el-form-item>
                    <h2 class="font-bold text-base mb-1">评论设置</h2>
                </el-form-item>
                <el-form-item label="敏感词过滤">
                    <el-switch v-model="form.isCommentSensiWordOpen" inline-prompt :active-icon="Check" :inactive-icon="Close"
                    @change="sensiWordSwitchChange"/>
                    <div class="flex items-center ml-3">
                        <el-icon class="mr-2" color="#909399"><InfoFilled /></el-icon>
                        <el-text class="mx-1" type="info"  size="small">开启后，系统自动对发表的每条评论进行敏感词过滤</el-text>
                    </div>
                </el-form-item>
                <el-form-item label="开启审核">
                    <el-switch v-model="form.isCommentExamineOpen" inline-prompt :active-icon="Check" :inactive-icon="Close"
                    @change="examineSwitchChange"/>
                    <div class="flex items-center ml-3">
                        <el-icon class="mr-2" color="#909399"><InfoFilled /></el-icon>
                        <el-text class="mx-1" type="info"  size="small">开启后，评论需要博主后台审核通过后，才会展示出来</el-text>
                    </div>
                </el-form-item>
                <el-form-item label="博主邮箱">
                    <el-input v-model="form.mail" clearable placeholder="请输入博主邮箱地址" />
                    <div class="flex items-center">
                        <el-icon class="mr-2" color="#909399"><InfoFilled /></el-icon>
                        <el-text class="mx-1" type="info"  size="small">当被评论后，用于主动发送邮件通知博主</el-text>
                    </div>
                </el-form-item>
                <el-form-item label="评论图片大小上限(MB)">
                    <el-input-number v-model="form.commentImageMaxSizeMb" :min="1" :max="50" />
                </el-form-item>
                <el-form-item label="贴纸包上传上限(张)">
                    <el-input-number v-model="form.stickerZipMaxCount" :min="10" :max="500" />
                </el-form-item>
                <el-form-item label="评论链接预览">
                    <el-switch v-model="form.isLinkPreviewOpen" inline-prompt :active-icon="Check" :inactive-icon="Close" />
                </el-form-item>
                <el-form-item label="链接预览白名单" v-if="form.isLinkPreviewOpen">
                    <el-input v-model="form.linkPreviewWhitelist" type="textarea" rows="3" placeholder="每行一个域名，支持通配符如 *.example.com" />
                </el-form-item>

                <!-- 分割线 -->
                <el-divider />

                <el-form-item>
                    <h2 class="font-bold text-base mb-1">邮件通知设置</h2>
                </el-form-item>
                <el-form-item label="开启邮件通知">
                    <el-switch v-model="form.isEmailNotificationOpen" inline-prompt :active-icon="Check" :inactive-icon="Close" />
                    <div class="flex items-center ml-3">
                        <el-icon class="mr-2" color="#909399"><InfoFilled /></el-icon>
                        <el-text class="mx-1" type="info"  size="small">开启后，有新评论或新订阅时将通过邮件通知博主</el-text>
                    </div>
                </el-form-item>
                <template v-if="form.isEmailNotificationOpen">
                    <el-form-item label="SMTP 主机">
                        <el-input v-model="form.smtpHost" clearable placeholder="例如 smtp.qq.com" />
                    </el-form-item>
                    <el-form-item label="SMTP 端口">
                        <el-input-number v-model="form.smtpPort" :min="1" :max="65535" />
                    </el-form-item>
                    <el-form-item label="SMTP 用户名">
                        <el-input v-model="form.smtpUsername" clearable placeholder="SMTP 登录用户名" />
                    </el-form-item>
                    <el-form-item label="SMTP 密码">
                        <el-input v-model="form.smtpPassword" type="password" show-password clearable placeholder="SMTP 登录密码" />
                    </el-form-item>
                    <el-form-item label="发件人邮箱">
                        <el-input v-model="form.smtpFromEmail" clearable placeholder="例如 noreply@example.com" />
                    </el-form-item>
                    <el-form-item label="发件人名称">
                        <el-input v-model="form.smtpFromName" clearable placeholder="例如 前锦阁" />
                    </el-form-item>
                    <el-form-item label="启用 SSL">
                        <el-switch v-model="form.smtpEnableSsl" inline-prompt :active-icon="Check" :inactive-icon="Close" />
                    </el-form-item>
                </template>

                <!-- 分割线 -->
                <el-divider />

                <el-form-item>
                    <h2 class="font-bold text-base mb-1">订阅设置</h2>
                </el-form-item>
                <el-form-item label="展示订阅卡片">
                    <el-switch v-model="form.isSubscribeCardOpen" inline-prompt :active-icon="Check" :inactive-icon="Close" />
                </el-form-item>
                <template v-if="form.isSubscribeCardOpen">
                    <el-form-item label="订阅卡片标题">
                        <el-input v-model="form.subscribeTitle" clearable placeholder="订阅更新" />
                    </el-form-item>
                    <el-form-item label="订阅卡片说明">
                        <el-input v-model="form.subscribeDescription" type="textarea" rows="2" placeholder="订阅后，最新文章将通过邮件发送给你" />
                    </el-form-item>
                    <el-form-item label="订阅框占位文字">
                        <el-input v-model="form.subscribePlaceholder" clearable placeholder="输入你的邮箱地址" />
                    </el-form-item>
                    <el-form-item label="订阅按钮文字">
                        <el-input v-model="form.subscribeButtonText" clearable placeholder="订阅" />
                    </el-form-item>
                </template>

                <el-form-item>
                    <el-button type="primary" :loading="btnLoading" @click="onSubmit">保存</el-button>
                </el-form-item>
            </el-form>
        </el-card>
    </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { Check, Close } from '@element-plus/icons-vue'
import { getBlogSettingsDetail, updateBlogSettings } from '@/api/admin/blogsettings'
import { uploadFile } from '@/api/admin/file'
import { showMessage } from '@/composables/util'

// 是否开启 GitHub
const isGithubChecked = ref(false)
// 是否开启 Gitee
const isGiteeChecked = ref(false)
// 是否开启知乎
const isZhihuChecked = ref(false)
// 是否开启 CSDN
const isCSDNChecked = ref(false)
// 是否显示保存按钮的 loading 状态，默认为 false
const btnLoading = ref(false)

// 表单引用
const formRef = ref(null)
// 表单对象
const form = reactive({
    name: '',
    author: '',
    logo: '',
    avatar: '',
    introduction: '',
    githubHomepage: '',
    giteeHomepage: '',
    zhihuHomepage: '',
    csdnHomepage: '',
    isCommentSensiWordOpen: true, // 是否开启评论敏感词过滤
    isCommentExamineOpen: false, // 是否开启评论审核
    mail: '', // 博主邮箱
    commentImageMaxSizeMb: 5, // 评论图片大小上限
    stickerZipMaxCount: 100, // 贴纸包上传上限
    isLinkPreviewOpen: false, // 是否开启链接预览
    linkPreviewWhitelist: '', // 链接预览白名单
    isEmailNotificationOpen: false, // 是否开启邮件通知
    smtpHost: '',
    smtpPort: 465,
    smtpUsername: '',
    smtpPassword: '',
    smtpEnableSsl: true,
    smtpFromEmail: '',
    smtpFromName: '',
    isSubscribeCardOpen: false, // 是否展示订阅卡片
    subscribeTitle: '',
    subscribeDescription: '',
    subscribePlaceholder: '',
    subscribeButtonText: ''
})

// 规则校验
const rules = {
    name: [{ required: true, message: '请输入博客名称', trigger: 'blur' }],
    author: [{ required: true, message: '请输入作者名', trigger: 'blur' }],
    logo: [{ required: true, message: '请上传博客 LOGO', trigger: 'blur' }],
    avatar: [{ required: true, message: '请上传作者头像', trigger: 'blur' }],
    introduction: [{ required: true, message: '请输入介绍语', trigger: 'blur' }],
}

// 监听 Github Switch 改变事件
const githubSwitchChange = (checked) => {
    if (checked == false) {
        form.githubHomepage = ''
    }
}

// 监听 Gitee Switch 改变事件
const giteeSwitchChange = (checked) => {
    if (checked == false) {
        form.giteeHomepage = ''
    }
}

// 监听知乎 Switch 改变事件
const zhihuSwitchChange = (checked) => {
    if (checked == false) {
        form.zhihuHomepage = ''
    }
}

// 监听 CSDN Switch 改变事件
const csdnSwitchChange = (checked) => {
    if (checked == false) {
        form.csdnHomepage = ''
    }
}

// 初始化博客设置数据，并渲染到页面上
function initBlogSettings() {
    getBlogSettingsDetail().then((e) => {
        if (e.success) {
            // 设置表单数据
            form.name = e.data.name
            form.author = e.data.author
            form.logo = e.data.logo
            form.avatar = e.data.avatar
            form.introduction = e.data.introduction

            // 第三方平台信息设置
            if (e.data.githubHomepage) {
                isGithubChecked.value = true
                form.githubHomepage = e.data.githubHomepage
            }

            if (e.data.giteeHomepage) {
                isGiteeChecked.value = true
                form.giteeHomepage = e.data.giteeHomepage
            }

            if (e.data.zhihuHomepage) {
                isZhihuChecked.value = true
                form.zhihuHomepage = e.data.zhihuHomepage
            }

            if (e.data.csdnHomepage) {
                isCSDNChecked.value = true
                form.csdnHomepage = e.data.csdnHomepage
            }

            form.isCommentSensiWordOpen = e.data.isCommentSensiWordOpen
            form.isCommentExamineOpen = e.data.isCommentExamineOpen
            form.mail = e.data.mail
            form.commentImageMaxSizeMb = e.data.commentImageMaxSizeMb ?? 5
            form.stickerZipMaxCount = e.data.stickerZipMaxCount ?? 100
            form.isLinkPreviewOpen = e.data.isLinkPreviewOpen ?? false
            form.linkPreviewWhitelist = e.data.linkPreviewWhitelist ?? ''
            form.isEmailNotificationOpen = e.data.isEmailNotificationOpen ?? false
            form.smtpHost = e.data.smtpHost ?? ''
            form.smtpPort = e.data.smtpPort ?? 465
            form.smtpUsername = e.data.smtpUsername ?? ''
            form.smtpPassword = e.data.smtpPassword ?? ''
            form.smtpEnableSsl = e.data.smtpEnableSsl ?? true
            form.smtpFromEmail = e.data.smtpFromEmail ?? ''
            form.smtpFromName = e.data.smtpFromName ?? ''
            form.isSubscribeCardOpen = e.data.isSubscribeCardOpen ?? false
            form.subscribeTitle = e.data.subscribeTitle ?? ''
            form.subscribeDescription = e.data.subscribeDescription ?? ''
            form.subscribePlaceholder = e.data.subscribePlaceholder ?? ''
            form.subscribeButtonText = e.data.subscribeButtonText ?? ''
        }
    })
}
initBlogSettings()

// 上传 logo 图片
const handleLogoChange = (file) => {
    // 表单对象
    let formData = new FormData()
    // 添加 file 字段，并将文件传入 
    formData.append('file', file.raw)
    uploadFile(formData).then((e) => {
        // 响参失败，提示错误消息
        if (e.success == false) {
            let message = e.message
            showMessage(message, 'error')
            return
        }

        // 成功则设置 logo 链接，并提示成功
        form.logo = e.data
        showMessage('上传成功')
    })
}

// 上传作者头像
const handleAvatarChange = (file) => {
    // 表单对象
    let formData = new FormData()
    // 添加 file 字段，并将文件传入 
    formData.append('file', file.raw)
    uploadFile(formData).then((e) => {
        // 响参失败，提示错误消息
        if (e.success == false) {
            let message = e.message
            showMessage(message, 'error')
            return
        }

        // 成功则设置作者头像链接，并提示成功
        form.avatar = e.data
        showMessage('上传成功')
    })
}

// 保存当前博客设置
const onSubmit = () => {
    // 先验证 form 表单字段
    formRef.value.validate((valid) => {
        if (!valid) {
            console.log('表单验证不通过')
            return false
        }

        // 显示保存按钮 loading
        btnLoading.value = true
        updateBlogSettings(form).then((res) => {
            if (res.success == false) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }
            
            // 重新渲染页面中的信息
            initBlogSettings()
            showMessage('保存成功')
        }).finally(() => btnLoading.value = false) // 隐藏保存按钮 loading
    })
}

// 评论敏感词过滤 switch 组件 change 事件
const sensiWordSwitchChange = (checked) => form.isCommentSensiWordOpen = checked
// 评论审核 switch 组件 change 事件
const examineSwitchChange = (checked) => form.isCommentExamineOpen = checked


</script>

<style scoped>
.avatar-uploader .avatar {
    width: 100px;
    height: 100px;
    display: block;
}
</style>

<style>
/* 解决 textarea :focus 状态下，边框消失的问题 */
.el-textarea__inner:focus {
    outline: 0 !important;
    box-shadow: 0 0 0 1px var(--el-input-focus-border-color) inset !important;
}

.avatar-uploader .el-upload {
    border: 1px dashed var(--el-border-color);
    border-radius: 6px;
    cursor: pointer;
    position: relative;
    overflow: hidden;
    transition: var(--el-transition-duration-fast);
}

.avatar-uploader .el-upload:hover {
    border-color: var(--el-color-primary);
}

.el-icon.avatar-uploader-icon {
    font-size: 28px;
    color: #8c939d;
    width: 100px;
    height: 100px;
    text-align: center;
}
</style>