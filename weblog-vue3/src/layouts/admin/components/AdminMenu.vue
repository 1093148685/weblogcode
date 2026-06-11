<template>
    <div class="menu-container h-full flex flex-col overflow-y-auto"
         :style="{ width: menuStore.menuWidth }">

        <!-- 顶部 Logo -->
        <div class="logo-area flex items-center justify-center h-[56px] cursor-pointer flex-shrink-0" @click="router.push('/')">
            <div v-if="menuStore.menuWidth == '250px'" class="flex items-center gap-2">
                <span class="logo-text text-xl font-bold">Weblog</span>
            </div>
            <div v-else class="flex items-center justify-center w-full">
                <span class="logo-text text-lg font-bold">W</span>
            </div>
        </div>

        <div class="menu-divider mx-3"></div>

        <!-- 下方菜单 -->
        <div class="flex-1 overflow-y-auto px-2 py-2">
            <el-menu
                :default-active="defaultActive"
                @select="handleSelect"
                :collapse="isCollapse"
                :collapse-transition="false"
            >
                <template v-for="(item, index) in menus" :key="index">
                    <el-sub-menu v-if="item.children" :index="item.path">
                        <template #title>
                            <el-icon><component :is="item.icon"></component></el-icon>
                            <span>{{ item.name }}</span>
                        </template>
                        <el-menu-item
                            v-for="child in item.children"
                            :key="child.path"
                            :index="child.path"
                        >
                            {{ child.name }}
                        </el-menu-item>
                    </el-sub-menu>
                    <el-menu-item v-else :index="item.path">
                        <el-icon><component :is="item.icon"></component></el-icon>
                        <template #title>
                            <span>{{ item.name }}</span>
                        </template>
                    </el-menu-item>
                </template>
            </el-menu>
        </div>

        <!-- 底部版本信息 -->
        <div v-if="menuStore.menuWidth == '250px'" class="menu-footer flex-shrink-0 px-4 py-2 text-center">
            <span class="text-xs" style="color: var(--admin-sidebar-text-muted);">Weblog Admin v1.0</span>
        </div>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useMenuStore } from '@/stores/menu'

const menuStore = useMenuStore()
const route = useRoute()
const router = useRouter()

const isCollapse = computed(() => !(menuStore.menuWidth == '250px'))
const defaultActive = ref(route.path)

const handleSelect = (path) => {
    router.push(path)
}

const menus = [
    { name: '仪表盘', icon: 'Monitor', path: '/admin/index' },
    { name: '文章管理', icon: 'Document', path: '/admin/article/list' },
    { name: '分类管理', icon: 'FolderOpened', path: '/admin/category/list' },
    { name: '标签管理', icon: 'PriceTag', path: '/admin/tag/list' },
    { name: '知识库管理', icon: 'Collection', path: '/admin/wiki/list' },
    {
        name: '评论管理', icon: 'ChatDotSquare', path: '/admin/comment/list',
        children: [
            { name: '评论列表', path: '/admin/comment/list' },
            { name: '私密评论', path: '/admin/comment/secret' },
            { name: '贴纸管理', path: '/admin/sticker' },
        ]
    },
    { name: '博客设置', icon: 'Setting', path: '/admin/blog/settings' },
]
</script>

<style>
/* ===== 侧边栏容器 ===== */
.menu-container {
    background: var(--admin-sidebar-bg);
    border-right: 1px solid var(--admin-sidebar-border);
}

/* ===== Logo 区域 ===== */
.logo-area {
    background: var(--admin-sidebar-logo-bg);
    border-bottom: 1px solid var(--admin-sidebar-border);
}

.logo-text {
    background: linear-gradient(135deg, #f8fafc, #67e8f9 48%, #a78bfa);
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
}

.menu-divider {
    height: 1px;
    flex-shrink: 0;
    background: var(--admin-sidebar-border);
}

/* ===== Element Plus el-menu overrides ===== */
.menu-container .el-menu {
    background-color: transparent;
    border-right: none;
}

.menu-container .el-menu-item,
.menu-container .el-sub-menu__title {
    color: var(--admin-sidebar-text-muted);
    border-radius: 8px;
    margin: 2px 0;
    height: 42px;
    line-height: 42px;
}

.menu-container .el-menu-item:hover,
.menu-container .el-sub-menu__title:hover {
    background-color: var(--admin-sidebar-item-hover);
    color: var(--admin-sidebar-text);
}

.menu-container .el-menu-item.is-active {
    background-color: var(--admin-sidebar-item-active-bg);
    color: var(--admin-sidebar-item-active-text);
    font-weight: 500;
}

.menu-container .el-sub-menu.is-opened > .el-sub-menu__title {
    color: var(--admin-sidebar-text);
}

.menu-container .el-sub-menu .el-menu {
    background-color: transparent;
}

.menu-container .el-sub-menu .el-menu-item {
    padding-left: 48px;
    height: 36px;
    line-height: 36px;
    font-size: 13px;
}

.menu-container .el-menu-item .el-icon,
.menu-container .el-sub-menu__title .el-icon {
    font-size: 17px;
    margin-right: 4px;
}

/* ===== 底部 ===== */
.menu-footer {
    border-top: 1px solid var(--admin-sidebar-border);
    background: rgba(0, 0, 0, 0.2);
}
</style>
