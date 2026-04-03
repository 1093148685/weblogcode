<template>
    <!-- 固钉组件 -->
    <el-affix :offset="0">
        <!-- Header 主体 -->
        <div class="admin-header h-[64px] flex items-center pr-4 border-b border-slate-200/60">

            <!-- 左边：菜单折叠按钮 -->
            <div
                class="header-btn w-[46px] h-[64px] cursor-pointer flex items-center justify-center"
                @click="handleMenuWidth"
                title="折叠菜单"
            >
                <el-icon class="text-slate-500 text-[18px]">
                    <Fold v-if="menuStore.menuWidth == '250px'" />
                    <Expand v-else />
                </el-icon>
            </div>

            <!-- 面包屑区域 -->
            <div class="flex-1 hidden md:flex items-center ml-2">
                <el-breadcrumb separator="/">
                    <el-breadcrumb-item :to="{ path: '/admin/index' }">首页</el-breadcrumb-item>
                    <el-breadcrumb-item v-if="currentPageTitle" class="text-slate-600">{{ currentPageTitle }}</el-breadcrumb-item>
                </el-breadcrumb>
            </div>

            <!-- 右边工具栏 -->
            <div class="ml-auto flex items-center gap-1">

                <!-- 刷新 -->
                <el-tooltip effect="dark" content="刷新页面" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="handleRefresh">
                        <el-icon class="text-slate-500 text-[16px]"><Refresh /></el-icon>
                    </div>
                </el-tooltip>

                <!-- 跳转前台 -->
                <el-tooltip effect="dark" content="访问前台" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="router.push('/')">
                        <el-icon class="text-slate-500 text-[16px]"><Monitor /></el-icon>
                    </div>
                </el-tooltip>

                <!-- 全屏 -->
                <el-tooltip effect="dark" :content="isFullscreen ? '退出全屏' : '全屏'" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="toggle">
                        <el-icon class="text-slate-500 text-[16px]">
                            <FullScreen v-if="!isFullscreen" />
                            <Aim v-else />
                        </el-icon>
                    </div>
                </el-tooltip>

                <!-- 夜晚模式 -->
                <el-tooltip effect="dark" :content="themeStore.mode === 'dark' ? '切换白天模式' : '切换夜晚模式'" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg theme-btn" @click="themeStore.toggle()">
                        <el-icon class="text-[16px]">
                            <Sunny v-if="themeStore.mode === 'dark'" />
                            <Moon v-else />
                        </el-icon>
                    </div>
                </el-tooltip>

                <!-- 分割线 -->
                <div class="w-px h-5 bg-slate-200 mx-1"></div>

                <!-- 用户头像下拉 -->
                <el-dropdown class="flex items-center" @command="handleCommand" trigger="click">
                    <div class="user-trigger flex items-center gap-2 px-3 py-1.5 rounded-lg cursor-pointer transition-all duration-200">
                        <el-avatar :size="28" src="src/assets/avr.jpg" class="ring-2 ring-indigo-200/60" />
                        <span class="text-slate-600 text-sm font-medium hidden sm:block">{{ userStore.userInfo.username }}</span>
                        <el-icon class="text-slate-400 text-xs"><arrow-down /></el-icon>
                    </div>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item command="updatePassword">
                                <el-icon class="mr-1"><EditPen /></el-icon>
                                修改密码
                            </el-dropdown-item>
                            <el-dropdown-item command="logout" divided>
                                <el-icon class="mr-1"><SwitchButton /></el-icon>
                                退出登录
                            </el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>
        </div>

        <!-- 修改密码对话框 -->
        <FormDialog ref="formDialogRef" title="修改密码" destroyOnClose @submit="onSubmit">
            <el-form ref="formRef" :rules="rules" :model="form">
                <el-form-item label="用户名" prop="username" label-width="120px" size="large">
                    <el-input v-model="form.username" placeholder="请输入用户名" clearable disabled />
                </el-form-item>
                <el-form-item label="新密码" prop="password" label-width="120px" size="large">
                    <el-input type="password" v-model="form.password" placeholder="请输入新密码" clearable show-password />
                </el-form-item>
                <el-form-item label="确认新密码" prop="rePassword" label-width="120px" size="large">
                    <el-input type="password" v-model="form.rePassword" placeholder="请确认新密码" clearable show-password />
                </el-form-item>
            </el-form>
        </FormDialog>
    </el-affix>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useMenuStore } from '@/stores/menu'
import { useUserStore } from '@/stores/user'
import { useThemeStore } from '@/stores/theme'
import { useFullscreen } from '@vueuse/core'
import { updateAdminPassword } from '@/api/admin/user'
import { showMessage, showModel } from '@/composables/util'
import { useRouter, useRoute } from 'vue-router'
import FormDialog from '@/components/FormDialog.vue'

const router = useRouter()
const route = useRoute()

const { isFullscreen, toggle } = useFullscreen()

const menuStore = useMenuStore()
const userStore = useUserStore()
const themeStore = useThemeStore()

// 当前页面标题（读取路由 meta）
const currentPageTitle = computed(() => route.meta?.title || '')

const handleMenuWidth = () => {
    menuStore.handleMenuWidth()
}

const handleRefresh = () => location.reload()

const formDialogRef = ref(false)

const handleCommand = (command) => {
    if (command == 'updatePassword') {
        formDialogRef.value.open()
    } else if (command == 'logout') {
        logout()
    }
}

function logout() {
    showModel('是否确认要退出登录？').then(() => {
        userStore.logout()
        showMessage('退出登录成功！')
        router.push('/login')
    })
}

const formRef = ref(null)
const form = reactive({
    username: userStore.userInfo.username || '',
    password: '',
    rePassword: ''
})

watch(() => userStore.userInfo.username, (newValue) => {
    form.username = newValue
})

const rules = {
    username: [{ required: true, message: '用户名不能为空', trigger: 'blur' }],
    password: [{ required: true, message: '密码不能为空', trigger: 'blur' }],
    rePassword: [{ required: true, message: '确认密码不能为空', trigger: 'blur' }]
}

const onSubmit = () => {
    formRef.value.validate((valid) => {
        if (!valid) return false
        if (form.password != form.rePassword) {
            showMessage('两次密码输入不一致，请检查！', 'warning')
            return
        }
        formDialogRef.value.showBtnLoading()
        updateAdminPassword(form).then((res) => {
            if (res.success == true) {
                showMessage('密码重置成功，请重新登录！')
                userStore.logout()
                formDialogRef.value.close()
                router.push('/login')
            } else {
                showMessage(res.message, 'error')
            }
        }).finally(() => formDialogRef.value.closeBtnLoading())
    })
}
</script>

<style scoped>
/* ===== Header 背景（参考 FeiTwnd 简约白卡风格） ===== */
.admin-header {
    background: #ffffff;
    box-shadow: 0 1px 0 rgba(0, 0, 0, 0.06);
}

/* ===== 工具栏按钮 ===== */
.header-btn {
    border-radius: 8px;
    transition: all 0.18s ease;
}

.header-btn:hover {
    background: #f1f5f9;
}

.header-btn:hover :deep(.el-icon) {
    color: #6366f1 !important;
}

/* ===== 用户触发区域 ===== */
.user-trigger {
    border-radius: 8px;
    transition: all 0.18s ease;
}

.user-trigger:hover {
    background: #f1f5f9;
}

/* ===== 面包屑 ===== */
:deep(.el-breadcrumb__inner) {
    color: #94a3b8;
    font-size: 13px;
}

:deep(.el-breadcrumb__inner.is-link:hover) {
    color: #6366f1;
}

:deep(.el-breadcrumb__separator) {
    color: #cbd5e1;
}
</style>
