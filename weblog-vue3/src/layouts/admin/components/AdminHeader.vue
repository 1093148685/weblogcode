<template>
    <!-- 固钉组件 -->
    <el-affix :offset="0">
        <!-- Header 主体 -->
        <div class="admin-header h-[64px] flex items-center pr-4">

            <!-- 左边：菜单折叠按钮 -->
            <div
                class="header-btn w-[46px] h-[64px] cursor-pointer flex items-center justify-center"
                @click="handleMenuWidth"
                title="折叠菜单"
            >
                <el-icon class="header-icon text-[18px]">
                    <Fold v-if="menuStore.menuWidth == '250px'" />
                    <Expand v-else />
                </el-icon>
            </div>

            <!-- 面包屑区域 -->
            <div class="flex-1 hidden md:flex items-center ml-2">
                <el-breadcrumb separator="/">
                    <el-breadcrumb-item :to="{ path: '/admin/index' }">控制台</el-breadcrumb-item>
                    <el-breadcrumb-item v-if="currentPageTitle">{{ currentPageTitle }}</el-breadcrumb-item>
                </el-breadcrumb>
            </div>

            <!-- 右边工具栏 -->
            <div class="ml-auto flex items-center gap-1">

                <!-- 刷新 -->
                <el-tooltip effect="dark" content="刷新页面" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="handleRefresh">
                        <el-icon class="header-icon text-[16px]"><Refresh /></el-icon>
                    </div>
                </el-tooltip>

                <!-- 跳转前台 -->
                <el-tooltip effect="dark" content="访问前台" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="router.push('/')">
                        <el-icon class="header-icon text-[16px]"><Monitor /></el-icon>
                    </div>
                </el-tooltip>

                <!-- 全屏 -->
                <el-tooltip effect="dark" :content="isFullscreen ? '退出全屏' : '全屏'" placement="bottom">
                    <div class="header-btn w-[38px] h-[38px] cursor-pointer flex items-center justify-center rounded-lg" @click="toggle">
                        <el-icon class="header-icon text-[16px]">
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
                <div class="header-divider w-px h-5 mx-1"></div>

                <!-- 用户头像下拉 -->
                <el-dropdown class="flex items-center" @command="handleCommand" trigger="click">
                    <div class="user-trigger flex items-center gap-2 px-3 py-1.5 rounded-lg cursor-pointer transition-all duration-200">
                        <el-avatar :size="28" src="src/assets/avr.jpg" class="user-avatar" />
                        <span class="user-name text-sm font-medium hidden sm:block">{{ userStore.userInfo.username }}</span>
                        <el-icon class="header-icon text-xs"><arrow-down /></el-icon>
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
.admin-header {
    background:
        linear-gradient(180deg, rgba(255,255,255,0.08), transparent),
        var(--admin-bg-card);
    border-bottom: 1px solid var(--admin-border);
    box-shadow: 0 14px 35px rgba(15, 23, 42, 0.08);
    backdrop-filter: blur(18px);
}

/* ===== 工具栏按钮 ===== */
.header-btn {
    border-radius: 8px;
    transition: all 0.18s ease;
    color: var(--admin-text-muted);
}

.header-btn:hover {
    background: var(--admin-bg-hover);
}

.header-btn:hover :deep(.el-icon) {
    color: var(--admin-accent-2) !important;
}

.header-icon {
    color: var(--admin-text-muted);
}

.header-divider {
    background: var(--admin-border);
}

/* ===== 用户触发区域 ===== */
.user-trigger {
    border-radius: 8px;
    transition: all 0.18s ease;
    color: var(--admin-text);
    border: 1px solid transparent;
}

.user-trigger:hover {
    background: var(--admin-bg-hover);
    border-color: var(--admin-border);
}

.user-avatar {
    box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.32);
}

.user-name {
    color: var(--admin-text);
}

/* ===== 面包屑 ===== */
:deep(.el-breadcrumb__inner) {
    color: var(--admin-text-muted);
    font-size: 13px;
}

:deep(.el-breadcrumb__inner.is-link:hover) {
    color: var(--admin-accent-2);
}

:deep(.el-breadcrumb__separator) {
    color: var(--admin-border-strong);
}
</style>
