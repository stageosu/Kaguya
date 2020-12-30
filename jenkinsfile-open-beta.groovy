pipeline {
    agent any
    stages {
        stage('Build') {
            steps {
                sh "docker build -t kaguya:${BUILD_NUMBER} ."
                sh "docker tag kaguya:${BUILD_NUMBER} kaguya:latest" 
            }
        }
        stage('Stop Old') {
            steps {
                catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
                    sh "docker stop kaguya"
                    sh "docker rm kaguya"
                } 
            }
        }
        stage('Deploy') {
            steps {
                sh "docker build -t kaguya:${BUILD_NUMBER} ."
                sh "docker tag kaguya:${BUILD_NUMBER} kaguya:latest"
                sh "docker run --name=kaguya --restart=always -p 6969:5000  -v /etc/kaguya/appsettings.json:/KaguyaApp/appsettings.json -d kaguya:latest"
            }
        }
    }
}