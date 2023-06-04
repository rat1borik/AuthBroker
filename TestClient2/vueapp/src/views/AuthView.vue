<template>
  <div class="about">
    <div v-if="loading">Loading...</div>
    <div v-else>
      Your Name is {{ name }}, e-mail is {{ email }}.<br />
      You have <b>{{ total }}</b> on your account
    </div>
  </div>
</template>

<script lang="js">
import { defineComponent } from 'vue';

export default defineComponent({
    data(){
      return {
        email:null,
        name:null,
        total:null,
        loading:true
      }
    },
    async created(){
        var resp = await fetch('/api/v1/auth', { method: 'POST',
              headers: {
                'Content-Type': 'application/json;charset=utf-8'
              },
              body: JSON.stringify({
                authCode : this.$route.query.auth_code
              })})

        let json = await resp.json(); // there's always a body
        console.log(json)
        if (resp.status >= 200 && resp.status < 300) {
          var resp2 = await fetch('/api/v1/account', { method: 'POST',
              headers: {
                'Content-Type': 'application/json;charset=utf-8'
              },
              body: JSON.stringify({
                accToken : json.authToken
              })})
           let json2 = await resp2.json(); // there's always a body
            console.log(json2)
            if (resp2.status >= 200 && resp2.status < 300) {
              this.loading = false
              this.email = json2.email
              this.name = json2.clientName
              this.total = json2.total
            }
        } else {
          console.error('failed auth')
        }
    },
    methods: {
    },
});
</script>
