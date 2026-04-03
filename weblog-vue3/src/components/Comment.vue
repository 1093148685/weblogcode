<template>
    <div class="mt-14">
        <h2 class="flex justify-center items-center mb-7 text-[var(--text-secondary)]">全部评论<span>({{ total }})</span></h2>
        <!-- 卡片 -->
        <div :class="props.customeCss">
            <!-- 评论发布表单 -->
            <form>
                <div class="flex gap-3">
                    <!-- 头像 -->
                    <div>
                        <img v-if="commentStore.userInfo.avatar && commentStore.userInfo.avatar.length > 0"
                            :src="commentStore.userInfo.avatar" class="w-10 h-10 rounded-full">
                        <svg v-else class="w-10 h-10 text-[var(--text-muted)]" aria-hidden="true"
                            xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20">
                            <path
                                d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                        </svg>
                    </div>
                    <!-- 昵称、邮箱、网址、评论内容 -->
                    <div class="grow">
                        <div class="flex items-center gap-5 flex-row">
                            <div class="flex basis-1/3">
                                <span
                                    class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                    昵称
                                </span>
                                <input @blur="onNicknameInputBlur" v-model="commentStore.userInfo.nickname"
                                    data-tooltip-target="nickname-tooltip-click" data-tooltip-trigger="click"
                                    type="text" id="website-admin"
                                    class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5"
                                    placeholder="必填">
                                <!-- 昵称 Tooltip -->
                                <div id="nickname-tooltip-click" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                    输入 QQ 号会自动获取昵称和头像
                                    <div class="tooltip-arrow" data-popper-arrow></div>
                                </div>
                            </div>
                            <div class="flex basis-1/3">
                                <span
                                    class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                    邮箱
                                </span>
                                <input v-model="commentStore.userInfo.mail" data-tooltip-target="mail-tooltip-click"
                                    data-tooltip-trigger="click" type="text" id="website-admin" class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5" placeholder="必填">
                                <!-- 邮箱 Tooltip -->
                                <div id="mail-tooltip-click" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                    收到回复将会发送到您的邮箱
                                    <div class="tooltip-arrow" data-popper-arrow></div>
                                </div>
                            </div>
                            <div class="flex basis-1/3">
                                <span
                                    class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                    网址
                                </span>
                                <input v-model="commentStore.userInfo.website"
                                    data-tooltip-target="website-tooltip-click" data-tooltip-trigger="click" type="text"
                                    id="website-admin" class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5"
                                    placeholder="选填">
                                <!-- 网址 Tooltip -->
                                <div id="website-tooltip-click" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                    可通过点击头像访问你的网站 (非必填)
                                    <div class="tooltip-arrow" data-popper-arrow></div>
                                </div>

                            </div>
                        </div>
                        <div
                            class="w-full mb-4 mt-4 border border-[var(--border-base)] rounded-lg bg-[var(--bg-base)]">
                            <div class="px-4 py-2 bg-[var(--bg-card)] rounded-t-lg">
                                <label for="comment" class="sr-only">Your comment</label>
                                <textarea id="comment" rows="4" v-model="commentForm.content"
                                    class="w-full px-0 text-sm text-[var(--text-heading)] bg-[var(--bg-card)] border-0 focus:ring-0 placeholder-[var(--text-placeholder)]"
                                    placeholder="发表一个友善的评论吧..." required></textarea>
                            </div>
                            <div class="flex items-center justify-between px-3 py-2 border-t border-[var(--border-base)]">
                                <div @click="onPublishCommentClick" class="inline-flex items-center py-2.5 px-4 text-xs font-medium text-center text-white bg-[var(--color-primary)] rounded-lg focus:ring-4 focus:ring-[var(--color-primary)] hover:opacity-90">
                                    发送
                                </div>
                                <div class="flex ps-0 space-x-1 rtl:space-x-reverse sm:ps-2">
                                    <!-- Emoji -->
                                    <div data-popover-target="popover-emoji" type="button"
                                        class="inline-flex justify-center items-center p-2 text-[var(--text-secondary)] rounded cursor-pointer hover:text-[var(--text-heading)] hover:bg-[var(--bg-hover)]">
                                        <svg class="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg"
                                            fill="none" viewBox="0 0 24 24">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                                stroke-width="2"
                                                d="M15 9h0M9 9h0m12 3a9 9 0 1 1-18 0 9 9 0 0 1 18 0ZM7 13c0 1 .5 2.4 1.5 3.2a5.5 5.5 0 0 0 7 0c1-.8 1.5-2.2 1.5-3.2 0 0-2 1-5 1s-5-1-5-1Z" />
                                        </svg>
                                    </div>

                                    <!-- Emoji Popover -->
                                    <div data-popover id="popover-emoji" role="tooltip"
                                        class="absolute z-10 invisible inline-block w-64 text-sm text-[var(--text-secondary)] transition-opacity duration-300 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-lg shadow-sm opacity-0">
                                        <div class="p-2">
                                            <div class="grid grid-cols-6 gap-2">
                                                <div v-for="(emoji, index) in emojis" :key="index"
                                                    class="text-2xl hover:cursor-pointer" @click="addEmoji(emoji)">{{
            emoji }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </form>

            <!-- 评论列表 -->
            <div v-if="comments && comments.length > 0" v-for="(comment, index) in comments" :key="index">

                <!-- 边界线 -->
                <div v-if="index > 0" class="border-t ml-12 mt-5  border-[var(--border-base)]"></div>

                <!-- 一级评论 -->
                <div class="flex gap-3 mt-5">
                    <!-- 左边头像栏 -->
                    <div>
                        <a v-if="comment.website && comment.website.length > 0" @click="jumpToWebsite(comment.website)"
                            class="cursor-pointer">
                            <img v-if="comment.avatar && comment.avatar.length > 0" :src="comment.avatar"
                                class="w-10 h-10 rounded-full">
                            <svg v-else class="w-10 h-10 text-[var(--text-muted)] rounded-full"
                                aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                viewBox="0 0 20 20">
                                <path
                                    d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                            </svg>
                        </a>
                        <div v-else>
                            <img v-if="comment.avatar && comment.avatar.length > 0" :src="comment.avatar"
                                class="w-10 h-10 rounded-full">
                            <svg v-else class="w-10 h-10 text-[var(--text-muted)] rounded-full"
                                aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                viewBox="0 0 20 20">
                                <path
                                    d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                            </svg>
                        </div>
                    </div>
                    <!-- 右边评论信息 -->
                    <div class="flex flex-col gap-2 grow">
                        <!-- 昵称 -->
                        <div class="text-xs text-[#FB7299] font-bold">{{ comment.nickname }}</div>
                        <!-- 评论内容 -->
                        <div class="text-sm text-[var(--text-body)]">{{ comment.content }}</div>
                        <!-- Meta 信息 -->
                        <div class="flex items-center text-xs text-[var(--text-muted)]">
                            <!-- 发布时间 -->
                            <div>{{ comment.createTime }}</div>
                            <div class="text-[var(--text-muted)] cursor-pointer ml-4 hover:text-[var(--color-primary)]"
                                @click="showReplyForm(index, comment.nickname, comment.id, comment.id)">
                                回复
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 二级评论 -->
                <div class="ml-12" v-if="comment.childComments && comment.childComments.length > 0">
                    <div v-for="(childComment, index2) in comment.childComments" :key="index2">
                        <!-- 头像、昵称、评论内容 -->
                        <div class="flex items-center gap-3 mt-5">
                            <!-- 左边头像栏 -->
                            <div>
                                <a v-if="childComment.website && childComment.website.length > 0"
                                    @click="jumpToWebsite(childComment.website)" class="cursor-pointer">
                                    <img v-if="childComment.avatar && childComment.avatar.length > 0"
                                        :src="childComment.avatar" class="w-6 h-6 rounded-full">
                                    <svg v-else class="w-6 h-6 text-[var(--text-muted)] rounded-full"
                                        aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                        viewBox="0 0 20 20">
                                        <path
                                            d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                                    </svg>
                                </a>
                                <div v-else>
                                    <img v-if="childComment.avatar && childComment.avatar.length > 0"
                                        :src="childComment.avatar" class="w-6 h-6 rounded-full">
                                    <svg v-else class="w-6 h-6 text-[var(--text-muted)] rounded-full"
                                        aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                        viewBox="0 0 20 20">
                                        <path
                                            d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                                    </svg>
                                </div>
                            </div>
                            <!-- 昵称 -->
                            <div class="text-xs text-[#FB7299] font-bold">
                                {{ childComment.nickname }}
                                <!-- 【回复 @xxx】 -->
                                <span v-if="childComment.replyNickname" class="text-[var(--text-muted)] font-normal ml-1 mr-1">回复
                                    <span class="text-[var(--color-primary)] font-normal text-sm">@{{ childComment.replyNickname
                                        }}</span>
                                    <span class="text-[var(--text-muted)]"> :</span>
                                </span>
                            </div>
                            <!-- 评论内容 -->
                            <div class="text-sm text-[var(--text-body)]">{{ childComment.content }}</div>
                        </div>
                        <!-- Meta 信息 -->
                        <div class="ml-9 mt-1 flex items-center text-xs text-[var(--text-muted)]">
                            <!-- 发布时间 -->
                            <div>{{ childComment.createTime }}</div>
                            <div class="text-[var(--text-muted)] cursor-pointer ml-4 hover:text-[var(--color-primary)]"
                                @click="showReplyForm(index, childComment.nickname, childComment.id, comment.id)">
                                回复
                            </div>
                        </div>

                        <!-- 三级评论 -->
                        <div class="ml-12" v-if="childComment.childComments && childComment.childComments.length > 0">
                            <div v-for="(grandchildComment, index3) in childComment.childComments" :key="index3">
                                <div class="flex items-center gap-3 mt-5">
                                    <div>
                                        <a v-if="grandchildComment.website && grandchildComment.website.length > 0"
                                            @click="jumpToWebsite(grandchildComment.website)" class="cursor-pointer">
                                            <img v-if="grandchildComment.avatar && grandchildComment.avatar.length > 0"
                                                :src="grandchildComment.avatar" class="w-6 h-6 rounded-full">
                                            <svg v-else class="w-6 h-6 text-[var(--text-muted)] rounded-full"
                                                aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                                viewBox="0 0 20 20">
                                                <path d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                                            </svg>
                                        </a>
                                        <div v-else>
                                            <img v-if="grandchildComment.avatar && grandchildComment.avatar.length > 0"
                                                :src="grandchildComment.avatar" class="w-6 h-6 rounded-full">
                                            <svg v-else class="w-6 h-6 text-[var(--text-muted)] rounded-full"
                                                aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor"
                                                viewBox="0 0 20 20">
                                                <path d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                                            </svg>
                                        </div>
                                    </div>
                                    <div class="text-xs text-[#FB7299] font-bold">
                                        {{ grandchildComment.nickname }}
                                        <span v-if="grandchildComment.replyNickname" class="text-[var(--text-muted)] font-normal ml-1 mr-1">回复
                                            <span class="text-[var(--color-primary)] font-normal text-sm">@{{ grandchildComment.replyNickname }}</span>
                                            <span class="text-[var(--text-muted)]"> :</span>
                                        </span>
                                    </div>
                                    <div class="text-sm text-[var(--text-body)]">{{ grandchildComment.content }}</div>
                                </div>
                                <div class="ml-9 mt-1 flex items-center text-xs text-[var(--text-muted)]">
                                    <div>{{ grandchildComment.createTime }}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 二级评论回复表单 -->
                <form class="ml-12 mt-5" v-if="comment.isShowReplyForm">
                    <div class="flex gap-3">
                        <!-- 头像 -->
                        <div>
                            <img v-if="commentStore.userInfo.avatar && commentStore.userInfo.avatar.length > 0"
                                :src="commentStore.userInfo.avatar" class="w-10 h-10 rounded-full">
                            <svg v-else class="w-10 h-10 text-[var(--text-muted)]" aria-hidden="true"
                                xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20">
                                <path
                                    d="M10 0a10 10 0 1 0 10 10A10.011 10.011 0 0 0 10 0Zm0 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 13a8.949 8.949 0 0 1-4.951-1.488A3.987 3.987 0 0 1 9 13h2a3.987 3.987 0 0 1 3.951 3.512A8.949 8.949 0 0 1 10 18Z" />
                            </svg>
                        </div>
                        <!-- 昵称、邮箱、网址、评论内容 -->
                        <div class="grow">
                            <div class="flex items-center gap-5 flex-row">
                                <div class="flex basis-1/3">
                                    <span
                                        class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                        昵称
                                    </span>
                                    <input @blur="onNicknameInputBlur" v-model="commentStore.userInfo.nickname"
                                        data-tooltip-target="nickname-tooltip-click2" data-tooltip-trigger="click"
                                        type="text" id="website-admin"
                                        class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5"
                                        placeholder="必填">
                                    <!-- 昵称 Tooltip -->
                                    <div id="nickname-tooltip-click2" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                        输入 QQ 号会自动获取昵称和头像
                                        <div class="tooltip-arrow" data-popper-arrow></div>
                                    </div>
                                </div>
                                <div class="flex basis-1/3">
                                    <span
                                        class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                        邮箱
                                    </span>
                                    <input v-model="commentStore.userInfo.mail"
                                        data-tooltip-target="mail-tooltip-click2" data-tooltip-trigger="click"
                                        type="text" id="website-admin" class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5" placeholder="必填">
                                    <!-- 邮箱 Tooltip -->
                                    <div id="mail-tooltip-click2" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                        收到回复将会发送到您的邮箱
                                        <div class="tooltip-arrow" data-popper-arrow></div>
                                    </div>
                                </div>
                                <div class="flex basis-1/3">
                                    <span
                                        class="inline-flex border-e-0 items-center px-3 text-xs text-[var(--text-heading)] bg-[var(--bg-hover)] border rounded-e-0 border-[var(--border-base)] rounded-s-md">
                                        网址
                                    </span>
                                    <input v-model="commentStore.userInfo.website"
                                        data-tooltip-target="website-tooltip-click2" data-tooltip-trigger="click"
                                        type="text" id="website-admin" class="rounded-none rounded-e-lg border text-[var(--text-heading)] focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)] block flex-1 min-w-0 w-full text-xs border-[var(--border-base)] p-2.5"
                                        placeholder="选填">
                                    <!-- 网址 Tooltip -->
                                    <div id="website-tooltip-click2" role="tooltip" class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded-md shadow-sm opacity-0 tooltip">
                                        可通过点击头像访问你的网站 (非必填)
                                        <div class="tooltip-arrow" data-popper-arrow></div>
                                    </div>

                                </div>
                            </div>
                            <div
                                class="w-full mb-4 mt-4 border border-[var(--border-base)] rounded-lg bg-[var(--bg-base)]">
                                <div class="px-4 py-2 bg-[var(--bg-card)] rounded-t-lg">
                                    <label for="comment" class="sr-only">Your comment</label>
                                    <textarea id="comment" rows="4" v-model="replyContent"
                                        class="w-full px-0 text-sm text-[var(--text-heading)] bg-[var(--bg-card)] border-0 focus:ring-0 placeholder-[var(--text-placeholder)]"
                                        :placeholder="replyPlaceholderText" required></textarea>
                                </div>
                                <div class="flex items-center justify-between px-3 py-2 border-t border-[var(--border-base)]">
                                    <div @click="onReplyContentSubmit" class="inline-flex items-center py-2.5 px-4 text-xs font-medium text-center text-white bg-[var(--color-primary)] rounded-lg focus:ring-4 focus:ring-[var(--color-primary)] hover:opacity-90">
                                        发送
                                    </div>
                                    <div class="flex ps-0 space-x-1 rtl:space-x-reverse sm:ps-2">
                                        <!-- Emoji -->
                                        <div data-popover-target="popover-emoji2" type="button"
                                            class="inline-flex justify-center items-center p-2 text-[var(--text-secondary)] rounded cursor-pointer hover:text-[var(--text-heading)] hover:bg-[var(--bg-hover)]">
                                            <svg class="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg"
                                                fill="none" viewBox="0 0 24 24">
                                                <path stroke="currentColor" stroke-linecap="round"
                                                    stroke-linejoin="round" stroke-width="2"
                                                    d="M15 9h0M9 9h0m12 3a9 9 0 1 1-18 0 9 9 0 0 1 18 0ZM7 13c0 1 .5 2.4 1.5 3.2a5.5 5.5 0 0 0 7 0c1-.8 1.5-2.2 1.5-3.2 0 0-2 1-5 1s-5-1-5-1Z" />
                                            </svg>
                                        </div>

                                        <!-- Emoji Popover -->
                                        <div data-popover id="popover-emoji2" role="tooltip"
                                            class="absolute z-10 invisible inline-block w-64 text-sm text-[var(--text-secondary)] transition-opacity duration-300 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-lg shadow-sm opacity-0">
                                            <div class="p-2">
                                                <div class="grid grid-cols-6 gap-2">
                                                    <div v-for="(emoji, index) in emojis" :key="index"
                                                        class="text-2xl hover:cursor-pointer"
                                                        @click="addReplyEmoji(emoji)">
                                                        {{
                                                        emoji }}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </form>


            </div>
            <!-- 没有评论的提示文字 -->
            <div v-else class="flex items-center mt-10 mb-10 justify-center text-[var(--text-muted)]">还没有任何评论哟~</div>


        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted, nextTick } from 'vue'
import { useCommentStore } from '@/stores/comment'
import { getUserInfoByQQ, publishComment, getComments } from '@/api/frontend/comment'
import { useRoute } from 'vue-router'
import { showMessage } from '@/composables/util'

const route = useRoute()

const commentStore = useCommentStore()

// 对外暴露属性
const props = defineProps({
    customeCss: {
        type: String,
        default: 'w-full px-5 py-10 mb-3 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card'
    }
})

onMounted(() => {
})

// emojis 表情符号
const emojis = ref(['😃', '😁', '😅', '😂', '😍', '😜', '😝', '🤑', '🥵', '🥰', '😙', '😎'
    , '😵', '😭', '😱', '😖', '🥳', '👽', '🙈', '🤡', '😤', '💣', '💯', '💢', '❤️', '👍', '👏', '👋', '👌', '🤏', '🙏'])

// 评论表单
const commentForm = reactive({
    avatar: '',
    content: '',
    mail: '',
    nickname: '',
    routerUrl: route.path,
    website: '',
    replyCommentId: null,
    parentCommentId: null
})

// 初始化 commentForm 表单对象中的用户信息
function initFormCommentUserInfo() {
    commentForm.avatar = commentStore.userInfo.avatar
    commentForm.mail = commentStore.userInfo.mail
    commentForm.nickname = commentStore.userInfo.nickname
    commentForm.website = commentStore.userInfo.website
}
initFormCommentUserInfo()

// 邮箱正则
const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
// 一级评论发布点击事件
const onPublishCommentClick = () => {
    initFormCommentUserInfo()
    // 校验
    if (commentForm.nickname.length === 0) {
        showMessage('请填写 QQ 号或昵称', 'warning')
        return
    }
    if (commentForm.mail.length === 0 || !emailRegex.test(commentForm.mail)) {
        showMessage('邮箱格式不正确', 'warning')
        return
    }
    if (commentForm.content.length === 0) {
        showMessage('请填写评论内容', 'warning')
        return
    }

    publishComment(commentForm).then(res => {
        if (!res.success) {
            // 获取服务端返回的错误消息
            let message = res.message
            // 提示错误消息
            showMessage(message, 'error')
            return
        }

        // 检查是否有提示消息（如敏感词或待审核）
        if (res.data && res.data.message) {
            showMessage(res.data.message, 'warning')
        } else {
            showMessage('评论发布成功')
        }
        
        // 将表单对象中的 content 评论内容置空
        commentForm.content = ''
        // 重新渲染表单列表
        initComments()
    })
}

// 添加 Emoji 表情
const addEmoji = (emoji) => {
    commentForm.content = commentForm.content + emoji
}

// 昵称输入框 blur 事件
const onNicknameInputBlur = () => {
    let nickname = commentStore.userInfo.nickname
    // 校验昵称是否是纯数字
    if (!checkIfPureNumber(nickname)) {
        return
    }

    // 若是，请求后端接口，根据 QQ 号获取用户信息
    getUserInfoByQQ(nickname).then(res => {
        if (!res.success) {
            return
        }

        // 更新头像
        commentStore.userInfo.avatar = res.data.avatar
        commentStore.userInfo.mail = res.data.mail
        
        // 更新昵称（如果获取到了）
        if (res.data.nickname && res.data.nickname !== nickname) {
            commentStore.userInfo.nickname = res.data.nickname
        } else {
            // 如果无法自动获取昵称，提示用户手动输入
            showMessage('请手动输入昵称', 'warning')
        }
    })
}

// QQ 号校验，是否是纯数字 
function checkIfPureNumber(text) {
    const trimmedValue = text.trim();
    return /^\d+$/.test(trimmedValue);
}

// 评论数组
const comments = ref([])
// 评论总数量
const total = ref(0)

function initComments() {
    // 获取当前路由下的所有评论
    getComments(route.path).then(res => {
        if (res.success) {
            total.value = res.data.total
            comments.value = res.data.comments
        }
    })
}
initComments()

// 回复 textarea 的 placeholder 提示文字
const replyPlaceholderText = ref('发表一个友善的评论吧...')
// 回复的评论
const replyContent = ref('')

// 当前回复的评论 ID
const currReplyCommentId = ref(null)
// 当前回复的父级 ID
const currParentCommentId = ref(null)

// 展示回复表单
const showReplyForm = (index, nickname, replyCommentId, parentCommentId) => {
    currReplyCommentId.value = replyCommentId
    currParentCommentId.value = parentCommentId
    // 先将评论数组中一级评论的所有 isShowReplyForm 字段设置为 false
    comments.value.forEach(comment => comment.isShowReplyForm = false)
    // 拿到当前下标的评论
    let comment = comments.value[index]
    // isShowReplyForm 置为 true
    comment.isShowReplyForm = true
    // 动态设置评论回复表单中的 textarea 的 placeholder 提示文字
    replyPlaceholderText.value = '回复 @' + nickname + ':'

    nextTick(() => {
        // tooltips/popovers removed (flowbite dependency removed)
    })
}

// 评论回复表单：添加 Emoji
const addReplyEmoji = (emoji) => {
    replyContent.value = replyContent.value + emoji
}

// 评论回复发送事件
const onReplyContentSubmit = () => {
    initFormCommentUserInfo()
    // 校验
    if (commentForm.nickname.length === 0) {
        showMessage('请填写 QQ 号或昵称', 'warning')
        return
    }
    if (commentForm.mail.length === 0 || !emailRegex.test(commentForm.mail)) {
        showMessage('邮箱格式不正确', 'warning')
        return
    }
    if (replyContent.value.length === 0) {
        showMessage('请填写回复内容', 'warning')
        return
    }
    // 评论回复内容
    commentForm.content = replyContent.value
    commentForm.replyCommentId = currReplyCommentId.value
    commentForm.parentCommentId = currParentCommentId.value

    // 请求接口
    publishComment(commentForm).then(res => {
        if (!res.success) {
            // 获取服务端返回的错误消息
            let message = res.message
            // 提示错误消息
            showMessage(message, 'error')
            return
        }

        // 检查是否有提示消息（如敏感词或待审核）
        if (res.data && res.data.message) {
            showMessage(res.data.message, 'warning')
        } else {
            showMessage('回复评论成功')
        }
        
        // 将评论回复的内容置空
        replyContent.value = ''
        commentForm.content = ''
        // 重新渲染评论列表
        initComments()
    })
}

// 跳转评论用户网址
const jumpToWebsite = (url) => {
    // 在新窗口访问新的链接地址
    window.open(url.startsWith('http') ? url : 'http://' + url, '_blank');
}

</script>