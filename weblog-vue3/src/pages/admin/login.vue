<template>
    <div class="login-page">
        <section class="login-visual">
            <div class="visual-inner">
                <div class="visual-top">
                    <div class="brand-mark">W</div>
                    <div>
                        <p class="brand-kicker">Weblog Core</p>
                        <h1>ASP.NET Core 博客后台</h1>
                    </div>
                </div>

                <p class="visual-desc">
                    基于 ASP.NET Core 8、SqlSugar、Vue 3 构建，聚合内容管理、文件上传、AI 写作与 RAG 知识库能力。
                </p>

                <div class="stack-grid compact">
                    <div class="stack-card">
                        <span>Backend</span>
                        <strong>ASP.NET Core</strong>
                    </div>
                    <div class="stack-card">
                        <span>Data</span>
                        <strong>SqlSugar + MySQL</strong>
                    </div>
                    <div class="stack-card">
                        <span>AI</span>
                        <strong>RAG + Provider</strong>
                    </div>
                </div>

                <div class="feature-panel compact">
                    <span class="feature-dot cyan"></span>
                    <p>结构清晰，适合学习 Controller、Service、Repository 分层，也方便继续扩展 AI 能力。</p>
                </div>
            </div>
        </section>

        <section class="login-panel">
            <div class="login-card">
                <div class="login-card-header">
                    <p>后台登录</p>
                    <h2>欢迎回来</h2>
                    <span>登录后进入 ASP.NET Core + SqlSugar 管理控制台。</span>
                </div>

                <el-form class="login-form" ref="formRef" :rules="rules" :model="form">
                    <el-form-item prop="username">
                        <el-input size="large" v-model="form.username" placeholder="请输入管理员账号" :prefix-icon="User" clearable />
                    </el-form-item>
                    <el-form-item prop="password">
                        <el-input
                            size="large"
                            type="password"
                            v-model="form.password"
                            placeholder="请输入登录密码"
                            :prefix-icon="Lock"
                            clearable
                            show-password
                        />
                    </el-form-item>
                    <el-form-item>
                        <el-button class="login-button" size="large" :loading="loading" type="primary" @click="onSubmit">
                            进入管理后台
                        </el-button>
                    </el-form-item>
                </el-form>
            </div>

            <p class="login-footer">Weblog Core Admin · ASP.NET Core 8</p>
        </section>
    </div>
</template>

<script setup>
import { User, Lock } from '@element-plus/icons-vue'
import { login } from '@/api/admin/user'
import { ref, reactive, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { showMessage } from '@/composables/util'
import { setToken } from '@/composables/cookie'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

const form = reactive({
    username: 'test',
    password: 'test'
})

const router = useRouter()
const loading = ref(false)
const formRef = ref(null)

const rules = {
    username: [
        {
            required: true,
            message: '用户名不能为空',
            trigger: 'blur'
        }
    ],
    password: [
        {
            required: true,
            message: '密码不能为空',
            trigger: 'blur'
        }
    ]
}

const onSubmit = () => {
    formRef.value.validate((valid) => {
        if (!valid) {
            return false
        }

        loading.value = true

        login(form.username, form.password).then((res) => {
            if (res.success === true) {
                showMessage('登录成功')

                const token = res.data.token
                setToken(token)

                userStore.setUserInfo()
                router.push('/admin/index')
            } else {
                showMessage(res.message, 'error')
            }
        })
        .finally(() => {
            loading.value = false
        })
    })
}

function onKeyUp(e) {
    if (e.key === 'Enter') {
        onSubmit()
    }
}

onMounted(() => {
    document.addEventListener('keyup', onKeyUp)
})

onBeforeUnmount(() => {
    document.removeEventListener('keyup', onKeyUp)
})
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(0, 1.05fr) minmax(420px, 0.95fr);
  background:
    radial-gradient(circle at 16% 18%, rgba(45, 212, 191, 0.24), transparent 32rem),
    radial-gradient(circle at 72% 12%, rgba(99, 102, 241, 0.22), transparent 28rem),
    linear-gradient(135deg, #06111f 0%, #0f172a 48%, #111827 100%);
  color: #e5eefc;
  overflow: hidden;
}

.login-visual {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: clamp(32px, 6vw, 88px);
}

.login-visual::after {
  content: "";
  position: absolute;
  inset: auto 9% 8% auto;
  width: 280px;
  height: 280px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 999px;
  background: linear-gradient(145deg, rgba(56, 189, 248, 0.08), rgba(99, 102, 241, 0.08));
  pointer-events: none;
}

.visual-inner {
  position: relative;
  z-index: 1;
  animation: slide-in-left 0.72s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.visual-top {
  display: flex;
  gap: 18px;
  align-items: center;
  margin-bottom: 28px;
}

.brand-mark {
  width: 64px;
  height: 64px;
  display: grid;
  place-items: center;
  border-radius: 20px;
  font-size: 28px;
  font-weight: 800;
  color: #082f49;
  background: linear-gradient(135deg, #67e8f9, #60a5fa 55%, #a78bfa);
  box-shadow: 0 22px 55px rgba(59, 130, 246, 0.38);
}

.brand-kicker {
  margin: 0 0 6px;
  color: #67e8f9;
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0;
}

.visual-top h1 {
  margin: 0;
  max-width: 720px;
  font-size: clamp(34px, 5vw, 58px);
  line-height: 1.08;
  font-weight: 850;
  letter-spacing: 0;
}

.visual-desc {
  max-width: 720px;
  margin: 0 0 28px;
  color: #b6c4d8;
  font-size: 17px;
  line-height: 1.9;
}

.stack-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  max-width: 720px;
}

.stack-card {
  padding: 18px;
  border-radius: 16px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(15, 23, 42, 0.58);
  backdrop-filter: blur(14px);
  box-shadow: 0 18px 42px rgba(0, 0, 0, 0.12);
}

.stack-card span {
  display: block;
  margin-bottom: 8px;
  color: #7dd3fc;
  font-size: 13px;
}

.stack-card strong {
  color: #f8fafc;
  font-size: 16px;
}

.feature-panel {
  max-width: 720px;
  margin-top: 22px;
  padding: 18px 20px;
  display: flex;
  gap: 14px;
  align-items: flex-start;
  border-radius: 18px;
  border: 1px solid rgba(56, 189, 248, 0.22);
  background: rgba(8, 13, 26, 0.5);
}

.feature-dot {
  width: 10px;
  height: 10px;
  flex: 0 0 auto;
  margin-top: 8px;
  border-radius: 999px;
  background: #818cf8;
  box-shadow: 0 0 0 6px rgba(129, 140, 248, 0.14);
}

.feature-dot.cyan {
  background: #22d3ee;
  box-shadow: 0 0 0 6px rgba(34, 211, 238, 0.14);
}

.feature-panel p {
  margin: 0;
  color: #aab8cc;
  line-height: 1.8;
}

.login-panel {
  position: relative;
  display: flex;
  min-height: 100vh;
  flex-direction: column;
  justify-content: center;
  padding: 40px clamp(24px, 5vw, 72px);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.9), rgba(241, 245, 249, 0.96));
  color: #111827;
}

.login-card {
  width: min(100%, 460px);
  margin: 0 auto;
  padding: 34px;
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.88);
  border: 1px solid rgba(203, 213, 225, 0.78);
  box-shadow: 0 28px 90px rgba(15, 23, 42, 0.16);
  backdrop-filter: blur(18px);
  animation: slide-in-right 0.72s 0.08s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.login-card-header {
  margin-bottom: 26px;
}

.login-card-header p {
  margin: 0 0 10px;
  color: #2563eb;
  font-size: 14px;
  font-weight: 800;
}

.login-card-header h2 {
  margin: 0 0 10px;
  font-size: 34px;
  line-height: 1.15;
  font-weight: 850;
  color: #0f172a;
}

.login-card-header span {
  color: #64748b;
  line-height: 1.7;
}

.login-form {
  width: 100%;
}

:deep(.el-button--primary) {
  --el-button-bg-color: #2563eb;
  --el-button-border-color: #2563eb;
  --el-button-hover-bg-color: #0ea5e9;
  --el-button-hover-border-color: #0ea5e9;
  --el-button-active-bg-color: #1d4ed8;
  --el-button-active-border-color: #1d4ed8;
}

.login-button {
  width: 100%;
  margin-top: 8px;
  font-weight: 700;
  box-shadow: 0 16px 32px rgba(37, 99, 235, 0.24);
}

:deep(.el-input) {
  --el-input-focus-border-color: #2563eb;
  --el-input-hover-border-color: #60a5fa;
  --el-input-border-radius: 12px;
}

:deep(.el-input__wrapper) {
  min-height: 46px;
  border-radius: 12px;
  box-shadow: 0 0 0 1px rgba(203, 213, 225, 0.9) inset;
  background: rgba(248, 250, 252, 0.96);
}

.login-footer {
  margin: 22px auto 0;
  color: #94a3b8;
  font-size: 13px;
  animation: fade-up 0.65s 0.22s ease both;
}

@keyframes slide-in-left {
  from {
    opacity: 0;
    transform: translateX(-42px) scale(0.985);
  }
  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes slide-in-right {
  from {
    opacity: 0;
    transform: translateX(42px) scale(0.985);
  }
  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes fade-up {
  from {
    opacity: 0;
    transform: translateY(12px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 960px) {
  .login-page {
    min-height: auto;
    grid-template-columns: 1fr;
    overflow: auto;
  }

  .login-visual {
    min-height: auto;
    padding: 36px 24px 24px;
  }

  .visual-top h1 {
    font-size: 34px;
  }

  .login-panel {
    min-height: auto;
    padding: 32px 20px 42px;
  }
}

@media (max-width: 640px) {
  .visual-top {
    align-items: flex-start;
  }

  .stack-grid {
    grid-template-columns: 1fr;
  }

  .login-card {
    padding: 24px;
    border-radius: 20px;
  }

  .login-card-header h2 {
    font-size: 30px;
  }
}
</style>
