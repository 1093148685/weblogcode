<template>
    <!-- 外层容器 -->
    <el-container class="bg-slate-50/50">
    
        <!-- 左边侧边栏 -->
        <el-aside :width='menuStore.menuWidth' class="transition-all duration-300">
            <AdminMenu></AdminMenu>
        </el-aside>
        
        <!-- 右边主内容区域 -->
        <el-container>
            <!-- 顶栏容器 -->
            <el-header>
                <AdminHeader></AdminHeader>
            </el-header>
            
            <el-main class="bg-slate-100/80">
                <!-- 标签导航栏 -->
                <AdminTagList></AdminTagList>

                <!-- 主内容（根据路由动态展示不同页面） -->
                <router-view v-slot="{ Component, route }">
                    <Transition name="fade" mode="out-in">
                        <KeepAlive :include="cachedViews" :max="10" :key="route.path">
                            <component :is="Component"></component>
                        </KeepAlive>
                    </Transition>
                </router-view>
            </el-main>
            
            <!-- 底栏容器 -->
            <el-footer>
                <AdminFooter></AdminFooter>
            </el-footer>
        </el-container>
    </el-container>
</template>

<script setup>
// 引入组件
import AdminFooter from './components/AdminFooter.vue';
import AdminHeader from './components/AdminHeader.vue';
import AdminMenu from './components/AdminMenu.vue';
import AdminTagList from './components/AdminTagList.vue';
import { onMounted, computed } from 'vue';

import { useMenuStore } from '@/stores/menu'

const menuStore = useMenuStore()

const cachedViews = computed(() => {
    return [
        'AdminIndex',
        'AdminArticleList',
        'AdminCategoryList',
        'AdminTagList',
        'AdminBlogSettings',
        'AdminAnnouncement',
        'AdminAiModel',
        'AdminAiPlugin',
        'AdminAiProvider',
        'AdminWikiList',
        'AdminCommentList'
    ]
})

onMounted(() => {
    // 移除 html 标签中的 class="dark"
    document.documentElement.classList.remove('dark');
})
</script>

<style scoped>
.el-header {
    padding: 0!important;
}

.el-footer {
    padding: 0!important;
}

/* 内容区域过渡动画：淡入淡出 + 微移效果 */
.fade-enter-active {
    transition: all 0.25s ease-out;
}

.fade-leave-active {
    transition: all 0.15s ease-in;
}

.fade-enter-from {
    opacity: 0;
    transform: translateY(8px);
}

.fade-leave-to {
    opacity: 0;
}

/* 防止切换页面时白屏 */
.el-main {
    min-height: calc(100vh - 64px - 44px - 50px);
}
</style>