import { createRouter, createWebHashHistory } from 'vue-router'
import Admin from '@/layouts/admin/admin.vue'

// 统一在这里声明所有路由（懒加载，按需分包）
const routes = [
    {
        path: '/',
        component: () => import('@/pages/frontend/index.vue'),
        meta: { title: 'Weblog 首页' }
    },
    {
        path: '/archive/list',
        component: () => import('@/pages/frontend/archive-list.vue'),
        meta: { title: 'Weblog 归档页' }
    },
    {
        path: '/category/list',
        component: () => import('@/pages/frontend/category-list.vue'),
        meta: { title: 'Weblog 分类列表页' }
    },
    {
        path: '/category/article/list',
        component: () => import('@/pages/frontend/category-article-list.vue'),
        meta: { title: 'Weblog 分类文章页' }
    },
    {
        path: '/tag/list',
        component: () => import('@/pages/frontend/tag-list.vue'),
        meta: { title: 'Weblog 标签列表页' }
    },
    {
        path: '/tag/article/list',
        component: () => import('@/pages/frontend/tag-article-list.vue'),
        meta: { title: 'Weblog 标签文章页' }
    },
    {
        path: '/journal/list',
        component: () => import('@/pages/frontend/journal-list.vue'),
        meta: { title: 'Weblog 日志列表' }
    },
    {
        path: '/journal/:journalId',
        component: () => import('@/pages/frontend/journal-detail.vue'),
        meta: { title: 'Weblog 日志详情' }
    },
    {
        path: '/article/:articleId',
        component: () => import('@/pages/frontend/article-detail.vue'),
        meta: { title: 'Weblog 详情页' }
    },
    {
        path: '/wiki/list',
        component: () => import('@/pages/frontend/wiki-list.vue'),
        meta: { title: '知识库' }
    },
    {
        path: '/wiki/:wikiId',
        component: () => import('@/pages/frontend/wiki-detail.vue'),
        meta: { title: '知识库详情' }
    },
    {
        path: '/login',
        component: () => import('@/pages/admin/login.vue'),
        meta: { title: 'Weblog 登录页' }
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'NotFound',
        component: () => import('@/pages/frontend/404.vue'),
        meta: { title: '404 页' }
    },
    {
        path: "/admin",
        component: Admin,
        children: [
            {
                path: "/admin/index",
                component: () => import('@/pages/admin/index.vue'),
                meta: { title: '仪表盘' }
            },
            {
                path: "/admin/article/list",
                component: () => import('@/pages/admin/article-list.vue'),
                meta: { title: '文章管理' }
            },
            {
                path: "/admin/category/list",
                component: () => import('@/pages/admin/category-list.vue'),
                meta: { title: '分类管理' }
            },
            {
                path: "/admin/tag/list",
                component: () => import('@/pages/admin/tag-list.vue'),
                meta: { title: '标签管理' }
            },
            {
                path: "/admin/blog/settings",
                component: () => import('@/pages/admin/blog-settings.vue'),
                meta: { title: '博客设置' }
            },
            {
                path: "/admin/wiki/list",
                component: () => import('@/pages/admin/wiki-list.vue'),
                meta: { title: '知识库管理' }
            },
            {
                path: "/admin/comment/list",
                component: () => import('@/pages/admin/comment-list.vue'),
                meta: { title: '评论管理' }
            },
            {
                path: "/admin/announcement",
                component: () => import('@/pages/admin/announcement.vue'),
                meta: { title: '公告管理' }
            },
            {
                path: "/admin/sticker/manage",
                component: () => import('@/pages/admin/sticker-manager.vue'),
                meta: { title: '表情管理' }
            },
            {
                path: "/admin/comment/secret",
                component: () => import('@/pages/admin/secret-comment.vue'),
                meta: { title: '私密评论' }
            },
            {
                path: "/admin/journal/list",
                component: () => import('@/pages/admin/journal-list.vue'),
                meta: { title: '日志管理' }
            },
        ]
    }
]

const router = createRouter({
    history: createWebHashHistory(),
    routes,
    scrollBehavior() {
        return { top: 0 }
    }
})

export default router
