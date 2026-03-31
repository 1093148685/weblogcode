<template>
    <div class="fixed overflow-y-auto h-screen text-white menu-container transition-all duration-300 shadow-2xl bg-slate-900/80 backdrop-blur-md border-r border-slate-700/50" :style="{ width: menuStore.menuWidth }">
        <!-- 顶部 Logo, 指定高度为 64px, 和右边的 Header 头保持一样高 -->
        <div class="flex items-center justify-center h-[64px]" @click="router.push('/')">
            <img v-if="menuStore.menuWidth == '250px'" src="@/assets/weblog-logo.png" class="h-[60px]">
            <img v-else src="@/assets/weblog-logo-mini.png" class="h-[60px]">
        </div>

        <!-- 下方菜单 -->
        <el-menu :default-active="defaultActive" @select="handleSelect" :collapse="isCollapse" :collapse-transition="false">
            <template v-for="(item, index) in menus" :key="index">
                <el-sub-menu v-if="item.children" :index="item.path">
                    <template #title>
                        <el-icon>
                            <component :is="item.icon"></component>
                        </el-icon>
                        <span>{{ item.name }}</span>
                    </template>
                    <el-menu-item v-for="child in item.children" :key="child.path" :index="child.path">
                        <span>{{ child.name }}</span>
                    </el-menu-item>
                </el-sub-menu>
                <el-menu-item v-else :index="item.path">
                    <el-icon>
                        <component :is="item.icon"></component>
                    </el-icon>
                    <span>{{ item.name }}</span>
                </el-menu-item>
            </template>
        </el-menu>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useMenuStore } from '@/stores/menu'

const menuStore = useMenuStore()

const route = useRoute()
const router = useRouter()

// 是否折叠
const isCollapse = computed(() =>  !(menuStore.menuWidth == '250px'))

// 根据路由地址判断哪个菜单被选中
const defaultActive = ref(route.path)

// 菜单选择事件
const handleSelect = (path) => {
    router.push(path)
}

const menus = [
    {
        'name': '仪表盘',
        'icon': 'Monitor',
        'path': '/admin/index'
    },
    {
        'name': '文章管理',
        'icon': 'Document',
        'path': '/admin/article/list',
    },
    {
        'name': '分类管理',
        'icon': 'FolderOpened',
        'path': '/admin/category/list',
    },
    {
        'name': '标签管理',
        'icon': 'PriceTag',
        'path': '/admin/tag/list',
    },
    {
        'name': '知识库管理',
        'icon': 'Collection',
        'path': '/admin/wiki/list',
    },
    {
        'name': '评论管理',
        'icon': 'ChatDotSquare',
        'path': '/admin/comment/list',
        'children': [
            {
                'name': '评论列表',
                'path': '/admin/comment/list',
            },
            {
                'name': '私密评论',
                'path': '/admin/comment/secret',
            },
            {
                'name': '贴纸管理',
                'path': '/admin/sticker',
            }
        ]
    },
    {
        'name': '博客设置',
        'icon': 'Setting',
        'path': '/admin/blog/settings',
    },
    {
        'name': '公告管理',
        'icon': 'Bell',
        'path': '/admin/announcement',
    },
    {
        'name': 'AI 管理',
        'icon': 'Cpu',
        'path': '/admin/ai',
        'children': [
            {
                'name': 'AI Provider',
                'path': '/admin/ai-provider',
            },
            {
                'name': 'AI 插件',
                'path': '/admin/ai-plugin',
            },
            {
                'name': '智能 Agent',
                'path': '/admin/agent',
            },
        ]
    },
]
</script>

<style>
.el-menu {
    background-color: transparent;
    border-right: 0;
}

.el-sub-menu__title {
    color: #e2e8f0;
}

.el-sub-menu__title:hover {
    background-color: rgba(255, 255, 255, 0.1);
}

.el-sub-menu .el-menu {
    background-color: transparent;
}

.el-sub-menu .el-menu-item {
    background-color: transparent;
    color: #cbd5e1;
}

.el-sub-menu .el-menu-item:hover {
    background-color: rgba(255, 255, 255, 0.1);
    color: #fff;
}

.el-sub-menu .el-menu-item.is-active {
    background-color: rgba(59, 130, 246, 0.3);
    color: #fff;
    border-right: 2px solid #3b82f6;
}

.el-menu-item.is-active {
    background-color: rgba(59, 130, 246, 0.3);
    color: #fff;
    border-right: 2px solid #3b82f6;
}

.el-menu-item.is-active:hover {
    background-color: rgba(59, 130, 246, 0.4);
}

.el-menu-item {
    color: #cbd5e1;
}

.el-menu-item:hover {
    background-color: rgba(255, 255, 255, 0.1);
    color: #fff;
}
</style>
